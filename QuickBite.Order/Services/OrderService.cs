using MassTransit;
using QuickBite.Order.DTOs;
using QuickBite.Order.Entities;
using QuickBite.Order.Events;
using QuickBite.Order.Interfaces;
using System.Net.Http.Json;
using System.Text.Json;

namespace QuickBite.Order.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _repository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<OrderService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OrderService(
            IOrderRepository repository, 
            IHttpClientFactory httpClientFactory, 
            IPublishEndpoint publishEndpoint,
            ILogger<OrderService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _httpClientFactory = httpClientFactory;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<OrderResponseDto> PlaceOrderAsync(Guid customerId, PlaceOrderDto dto)
        {
            _logger.LogInformation("Attempting to place order for customer {CustomerId}", customerId);

            // 1. Fetch Cart from CartService
            var cartClient = _httpClientFactory.CreateClient("CartService");
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader))
            {
                cartClient.DefaultRequestHeaders.Add("Authorization", authHeader);
            }

            var cartResponse = await cartClient.GetAsync($"api/v1/cart"); 
            if (!cartResponse.IsSuccessStatusCode)
            {
                var errorBody = await cartResponse.Content.ReadAsStringAsync();
                throw new Exception($"Cart Service Error ({(int)cartResponse.StatusCode}): {errorBody}");
            }

            // Use case-insensitive deserialization: CartService returns camelCase JSON by default
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var cartJson = await cartResponse.Content.ReadAsStringAsync();
            _logger.LogInformation("Cart response body: {CartJson}", cartJson);
            var cart = JsonSerializer.Deserialize<InternalCartDto>(cartJson, jsonOptions);
            if (cart == null || cart.Items == null || !cart.Items.Any()) 
                throw new Exception("Your cart is empty. Please add items before placing an order.");

            // 2. Validate Restaurant Id and Total
            Guid restaurantId = cart.RestaurantId;
            decimal totalAmount = cart.GrandTotal;
            
            // 3. Create Order Entity
            var order = new Entities.Order
            {
                OrderId = Guid.NewGuid(),
                OrderNumber = "QB-" + DateTime.Now.ToString("yyyyMMdd") + "-" + new Random().Next(1000, 9999),
                CustomerId = customerId,
                RestaurantId = restaurantId,
                RestaurantName = cart.RestaurantName,
                TotalAmount = totalAmount,
                FinalAmount = totalAmount, // For now, ignoring complexity
                ModeOfPayment = dto.ModeOfPayment,
                Status = OrderStatus.PLACED,
                DeliveryAddress = dto.DeliveryAddress,
                SpecialInstructions = dto.SpecialInstructions
            };

            foreach (var item in cart.Items)
            {
                order.OrderItems.Add(new OrderItem
                {
                    OrderItemId = Guid.NewGuid(),
                    OrderId = order.OrderId,
                    MenuItemId = item.MenuItemId,
                    Name = item.Name,
                    Price = item.Price,
                    Quantity = item.Quantity,
                    Customization = item.Customization
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

        public async Task<IEnumerable<OrderResponseDto>> GetAgentOrdersAsync(Guid agentId)
        {
            var orders = await _repository.GetByAgentIdAsync(agentId);
            return orders.Select(MapToDto);
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

        public async Task<RestaurantStatsDto> GetRestaurantStatsAsync(Guid restaurantId)
        {
            var orders = await _repository.GetByRestaurantIdAsync(restaurantId);
            var today = DateTime.Today;

            var todayOrders = orders.Where(o => o.OrderDate.Date == today).ToList();
            
            var revenue = todayOrders
                .Where(o => o.Status != OrderStatus.CANCELLED)
                .Sum(o => o.FinalAmount);

            var activeCount = orders.Count(o => 
                o.Status != OrderStatus.DELIVERED && 
                o.Status != OrderStatus.CANCELLED);

            return new RestaurantStatsDto(revenue, activeCount, todayOrders.Count);
        }

        private void ValidateStatusTransition(OrderStatus current, OrderStatus next, string role)
        {
            bool isValid = (current, next) switch
            {
                (OrderStatus.PLACED, OrderStatus.CONFIRMED) when role == "OWNER" || role == "ADMIN" => true,
                (OrderStatus.CONFIRMED, OrderStatus.PREPARING) when role == "OWNER" => true,
                (OrderStatus.PREPARING, OrderStatus.READY) when role == "OWNER" => true,
                (OrderStatus.READY, OrderStatus.PICKED_UP) when role == "DELIVERY_AGENT" || role == "ADMIN" => true,
                (OrderStatus.PREPARING, OrderStatus.PICKED_UP) when role == "DELIVERY_AGENT" || role == "ADMIN" => true,
                (OrderStatus.PICKED_UP, OrderStatus.DELIVERED) when role == "DELIVERY_AGENT" => true,
                (OrderStatus.PLACED, OrderStatus.CANCELLED) => true,
                (OrderStatus.CONFIRMED, OrderStatus.CANCELLED) => true,
                _ => false
            };

            if (!isValid) throw new InvalidOperationException($"Invalid status transition from {current} to {next} for role {role}");
        }

        private OrderResponseDto MapToDto(Entities.Order o) => new OrderResponseDto(
            o.OrderId,
            o.RestaurantName,
            o.OrderNumber,
            o.RestaurantId,
            o.OrderItems.Select(i => new OrderItemDto(i.MenuItemId, i.Name, i.Price, i.Quantity, i.Customization)).ToList(),
            o.TotalAmount,
            o.Status.ToString(),
            o.OrderDate,
            o.DeliveryAddress,
            o.SpecialInstructions,
            o.DeliveryAgentId
        );

        public async Task SeedRestaurantOrdersAsync(Guid restaurantId, int count)
        {
            var random = new Random();
            for (int i = 0; i < count; i++)
            {
                var order = new Entities.Order
                {
                    OrderId = Guid.NewGuid(),
                    OrderNumber = "QB-SEED-" + DateTime.Now.ToString("MMdd") + "-" + random.Next(1000, 9999),
                    CustomerId = Guid.NewGuid(),
                    RestaurantId = restaurantId,
                    RestaurantName = "Seeded Outlet",
                    TotalAmount = random.Next(200, 800),
                    FinalAmount = 0, // Calculated below
                    ModeOfPayment = "WALLET",
                    Status = OrderStatus.PLACED,
                    DeliveryAddress = "Mock Address " + random.Next(1, 100),
                    OrderDate = DateTime.UtcNow
                };

                var item = new OrderItem
                {
                    OrderItemId = Guid.NewGuid(),
                    OrderId = order.OrderId,
                    MenuItemId = Guid.NewGuid(),
                    Name = "Seeded Dish " + (i + 1),
                    Price = order.TotalAmount,
                    Quantity = 1
                };
                order.OrderItems.Add(item);
                order.FinalAmount = order.TotalAmount;

                await _repository.AddAsync(order);
            }
            await _repository.SaveChangesAsync();
        }
    }

    // Internal Helper Classes for Cart Deserialization
    internal class InternalCartDto
    {
        public Guid RestaurantId { get; set; }
        public string RestaurantName { get; set; } = string.Empty;
        public decimal GrandTotal { get; set; }
        public List<InternalCartItemDto> Items { get; set; } = new();
    }

    internal class InternalCartItemDto
    {
        public Guid MenuItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? Customization { get; set; }
    }
}
