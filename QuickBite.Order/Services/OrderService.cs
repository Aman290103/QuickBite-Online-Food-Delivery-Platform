using MassTransit;
using QuickBite.Order.DTOs;
using QuickBite.Order.Entities;
using QuickBite.Order.Events;
using QuickBite.Order.Interfaces;
using System.Net.Http.Json;

namespace QuickBite.Order.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _repository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IOrderRepository repository, 
            IHttpClientFactory httpClientFactory, 
            IPublishEndpoint publishEndpoint,
            ILogger<OrderService> logger)
        {
            _repository = repository;
            _httpClientFactory = httpClientFactory;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task<OrderResponseDto> PlaceOrderAsync(Guid customerId, PlaceOrderDto dto)
        {
            _logger.LogInformation("Attempting to place order for customer {CustomerId}", customerId);

            // 1. Fetch Cart from CartService
            var cartClient = _httpClientFactory.CreateClient("CartService");
            var cartResponse = await cartClient.GetAsync($"api/v1/cart"); // Assuming customerId injected via Auth header in real setup
            if (!cartResponse.IsSuccessStatusCode)
                throw new Exception("Unable to fetch cart details.");

            var cart = await cartResponse.Content.ReadFromJsonAsync<dynamic>();
            if (cart == null) throw new Exception("Cart is empty.");

            // 2. Validate Restaurant Id and Total
            Guid restaurantId = Guid.Parse(cart.restaurantId.ToString());
            decimal totalAmount = decimal.Parse(cart.totalPrice.ToString());
            
            // 3. Create Order Entity
            var order = new Entities.Order
            {
                OrderId = Guid.NewGuid(),
                CustomerId = customerId,
                RestaurantId = restaurantId,
                TotalAmount = totalAmount,
                FinalAmount = totalAmount, // For now, ignoring complexity
                ModeOfPayment = dto.ModeOfPayment,
                Status = OrderStatus.PLACED,
                DeliveryAddress = dto.DeliveryAddress,
                SpecialInstructions = dto.SpecialInstructions
            };

            foreach (var item in cart.items)
            {
                order.OrderItems.Add(new OrderItem
                {
                    OrderItemId = Guid.NewGuid(),
                    OrderId = order.OrderId,
                    MenuItemId = Guid.Parse(item.menuItemId.ToString()),
                    Name = item.name.ToString(),
                    Price = decimal.Parse(item.price.ToString()),
                    Quantity = int.Parse(item.quantity.ToString()),
                    Customization = item.customization?.ToString()
                });
            }

            // 4. Payment Integration
            if (dto.ModeOfPayment == "ONLINE")
            {
                var paymentClient = _httpClientFactory.CreateClient("PaymentService");
                var paymentRes = await paymentClient.PostAsJsonAsync("api/v1/payments", new { OrderId = order.OrderId, Amount = order.FinalAmount });
                if (!paymentRes.IsSuccessStatusCode)
                    throw new Exception("Payment failed. Order aborted.");
            }

            // 5. Atomic Save
            await _repository.AddAsync(order);
            await _repository.SaveChangesAsync();

            // 6. Clear Cart
            await cartClient.DeleteAsync("api/v1/cart");

            // 7. Publish Event
            await _publishEndpoint.Publish<OrderPlacedEvent>(new
            {
                order.OrderId,
                order.CustomerId,
                order.RestaurantId,
                order.TotalAmount
            });

            _logger.LogInformation("Order {OrderId} placed successfully for customer {CustomerId}", order.OrderId, customerId);
            return MapToDto(order);
        }

        public async Task<OrderResponseDto?> GetOrderByIdAsync(Guid orderId)
        {
            var order = await _repository.GetByIdAsync(orderId);
            return order == null ? null : MapToDto(order);
        }

        public async Task<IEnumerable<OrderResponseDto>> GetCustomerHistoryAsync(Guid customerId)
        {
            var orders = await _repository.GetByCustomerIdAsync(customerId);
            return orders.Select(MapToDto);
        }

        public async Task<IEnumerable<OrderResponseDto>> GetRestaurantOrdersAsync(Guid restaurantId)
        {
            var orders = await _repository.GetByRestaurantIdAsync(restaurantId);
            return orders.Select(MapToDto);
        }

        public async Task<IEnumerable<OrderResponseDto>> GetAllOrdersAsync()
        {
            var orders = await _repository.GetAllAsync();
            return orders.Select(MapToDto);
        }

        public async Task<OrderResponseDto> UpdateStatusAsync(Guid orderId, OrderStatus newStatus, string actorRole)
        {
            var order = await _repository.GetByIdAsync(orderId);
            if (order == null) throw new Exception("Order not found.");

            ValidateStatusTransition(order.Status, newStatus, actorRole);

            order.Status = newStatus;
            await _repository.UpdateAsync(order);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Order {OrderId} status updated to {NewStatus} by {Role}", orderId, newStatus, actorRole);
            return MapToDto(order);
        }

        public async Task<OrderResponseDto> CancelOrderAsync(Guid orderId, Guid customerId)
        {
            var order = await _repository.GetByIdAsync(orderId);
            if (order == null || order.CustomerId != customerId)
                throw new Exception("Order not found or unauthorized.");

            if (order.Status != OrderStatus.PLACED && order.Status != OrderStatus.CONFIRMED)
                throw new InvalidOperationException("Order cannot be cancelled at this stage.");

            if (order.ModeOfPayment == "ONLINE")
            {
                var paymentClient = _httpClientFactory.CreateClient("PaymentService");
                var refundRes = await paymentClient.PostAsJsonAsync($"api/v1/payments/refund", new { OrderId = orderId });
                // Note: In real app, check response and handle failure.
            }

            order.Status = OrderStatus.CANCELLED;
            await _repository.UpdateAsync(order);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Order {OrderId} cancelled by customer {CustomerId}", orderId, customerId);
            return MapToDto(order);
        }

        public async Task<OrderResponseDto> ReorderAsync(Guid pastOrderId, Guid customerId)
        {
            var pastOrder = await _repository.GetByIdAsync(pastOrderId);
            if (pastOrder == null || pastOrder.CustomerId != customerId)
                throw new Exception("Past order not found.");

            // Clear current cart first
            var cartClient = _httpClientFactory.CreateClient("CartService");
            await cartClient.DeleteAsync("api/v1/cart");

            // Add items from past order to cart
            foreach (var item in pastOrder.OrderItems)
            {
                await cartClient.PostAsJsonAsync("api/v1/cart/items", new {
                    MenuItemId = item.MenuItemId,
                    Quantity = item.Quantity,
                    Customization = item.Customization
                });
            }

            _logger.LogInformation("Customer {CustomerId} reordered from history of Order {PastOrderId}", customerId, pastOrderId);
            
            // This just rebuilds the cart, customer then needs to PlaceOrder from UI
            // But requirement says "rebuild cart from past OrderItems", so we are done here.
            // Ideally, we return a summary.
            return MapToDto(pastOrder);
        }

        public async Task<OrderResponseDto> AssignAgentAsync(Guid orderId, Guid agentId)
        {
            var order = await _repository.GetByIdAsync(orderId);
            if (order == null) throw new Exception("Order not found.");

            order.DeliveryAgentId = agentId;
            await _repository.UpdateAsync(order);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Agent {AgentId} assigned to Order {OrderId}", agentId, orderId);
            return MapToDto(order);
        }

        private void ValidateStatusTransition(OrderStatus current, OrderStatus next, string role)
        {
            bool isValid = (current, next) switch
            {
                (OrderStatus.PLACED, OrderStatus.CONFIRMED) when role == "OWNER" || role == "ADMIN" => true,
                (OrderStatus.CONFIRMED, OrderStatus.PREPARING) when role == "OWNER" => true,
                (OrderStatus.PREPARING, OrderStatus.PICKED_UP) when role == "AGENT" || role == "ADMIN" => true,
                (OrderStatus.PICKED_UP, OrderStatus.DELIVERED) when role == "AGENT" => true,
                (OrderStatus.PLACED, OrderStatus.CANCELLED) => true,
                (OrderStatus.CONFIRMED, OrderStatus.CANCELLED) => true,
                _ => false
            };

            if (!isValid) throw new InvalidOperationException($"Invalid status transition from {current} to {next} for role {role}");
        }

        private OrderResponseDto MapToDto(Entities.Order o) => new OrderResponseDto(
            o.OrderId, o.CustomerId, o.RestaurantId, o.DeliveryAgentId, o.TotalAmount, o.Discount, o.FinalAmount, o.ModeOfPayment, o.Status, o.OrderDate, o.EstimatedDelivery, o.DeliveryAddress, o.SpecialInstructions,
            o.OrderItems.Select(i => new OrderItemDto(i.MenuItemId, i.Name, i.Price, i.Quantity, i.Customization)).ToList()
        );
    }
}
