using MassTransit;
using QuickBite.Order.DTOs;
using QuickBite.Order.Entities;
using QuickBite.Order.Events;
using QuickBite.Order.Interfaces;
using System.Net.Http.Json;
using System.Text.Json;

namespace QuickBite.Order.Services
{
    // [SERVICE: ORDER MANAGEMENT]
    // Coordinates the order lifecycle, payments, and notifications.
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
            var cartClient = _httpClientFactory.CreateClient("CartService");
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader)) cartClient.DefaultRequestHeaders.Add("Authorization", authHeader);

            var cartResponse = await cartClient.GetAsync($"api/v1/cart"); 
            if (!cartResponse.IsSuccessStatusCode) throw new Exception("Could not fetch cart.");

            var cart = await cartResponse.Content.ReadFromJsonAsync<InternalCartDto>();
            if (cart == null || !cart.Items.Any()) throw new Exception("Cart is empty.");

            var order = new Entities.Order
            {
                OrderId = Guid.NewGuid(),
                OrderNumber = "QB-" + DateTime.Now.ToString("yyyyMMdd") + "-" + new Random().Next(1000, 9999),
                CustomerId = customerId,
                RestaurantId = cart.RestaurantId,
                RestaurantName = cart.RestaurantName,
                TotalAmount = cart.GrandTotal,
                FinalAmount = cart.GrandTotal,
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

            if (dto.ModeOfPayment == "ONLINE")
            {
                var paymentClient = _httpClientFactory.CreateClient("PaymentService");
                var paymentRes = await paymentClient.PostAsJsonAsync("api/v1/payments", new { OrderId = order.OrderId, Amount = order.FinalAmount });
                if (!paymentRes.IsSuccessStatusCode) throw new Exception("Payment failed.");
            }

            await _repository.AddAsync(order);
            await _repository.SaveChangesAsync();
            await cartClient.DeleteAsync("api/v1/cart");

            await _publishEndpoint.Publish<OrderPlacedEvent>(new
            {
                order.OrderId,
                order.CustomerId,
                order.RestaurantId,
                order.TotalAmount,
                CustomerEmail = dto.CustomerEmail ?? "",
                CustomerPhone = dto.CustomerPhone ?? ""
            });

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
            order.Status = newStatus;
            await _repository.UpdateAsync(order);
            await _repository.SaveChangesAsync();
            return MapToDto(order);
        }

        public async Task<OrderResponseDto> CancelOrderAsync(Guid orderId, Guid customerId)
        {
            var order = await _repository.GetByIdAsync(orderId);
            if (order == null || order.CustomerId != customerId) throw new Exception("Unauthorized.");
            order.Status = OrderStatus.CANCELLED;
            await _repository.UpdateAsync(order);
            await _repository.SaveChangesAsync();
            return MapToDto(order);
        }

        public async Task<OrderResponseDto> ReorderAsync(Guid pastOrderId, Guid customerId)
        {
            var oldOrder = await _repository.GetByIdAsync(pastOrderId);
            if (oldOrder == null) throw new Exception("Original order not found.");

            var newOrder = new Entities.Order {
                OrderId = Guid.NewGuid(),
                OrderNumber = "QB-RE-" + DateTime.Now.ToString("yyyyMMdd"),
                CustomerId = customerId,
                RestaurantId = oldOrder.RestaurantId,
                RestaurantName = oldOrder.RestaurantName,
                TotalAmount = oldOrder.TotalAmount,
                FinalAmount = oldOrder.FinalAmount,
                Status = OrderStatus.PLACED,
                DeliveryAddress = oldOrder.DeliveryAddress
            };
            await _repository.AddAsync(newOrder);
            await _repository.SaveChangesAsync();
            return MapToDto(newOrder);
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
            return MapToDto(order);
        }

        public async Task<RestaurantStatsDto> GetRestaurantStatsAsync(Guid restaurantId)
        {
            var orders = await _repository.GetByRestaurantIdAsync(restaurantId);
            return new RestaurantStatsDto(
                orders.Where(o => o.Status == OrderStatus.DELIVERED).Sum(o => o.TotalAmount),
                orders.Count(o => o.Status != OrderStatus.DELIVERED),
                orders.Count()
            );
        }

        public async Task SeedRestaurantOrdersAsync(Guid restaurantId, int count)
        {
            await Task.CompletedTask;
        }

        private OrderResponseDto MapToDto(Entities.Order o) => new OrderResponseDto(
            o.OrderId, o.RestaurantName, o.OrderNumber, o.RestaurantId,
            o.OrderItems.Select(i => new OrderItemDto(i.MenuItemId, i.Name, i.Price, i.Quantity, i.Customization)).ToList(),
            o.TotalAmount, o.Status.ToString(), o.OrderDate, o.DeliveryAddress, o.SpecialInstructions, o.DeliveryAgentId
        );

        internal class InternalCartDto {
            public Guid RestaurantId { get; set; }
            public string RestaurantName { get; set; } = "";
            public decimal GrandTotal { get; set; }
            public List<InternalCartItemDto> Items { get; set; } = new();
        }
        internal class InternalCartItemDto {
            public Guid MenuItemId { get; set; }
            public string Name { get; set; } = "";
            public decimal Price { get; set; }
            public int Quantity { get; set; }
            public string? Customization { get; set; }
        }
    }
}
