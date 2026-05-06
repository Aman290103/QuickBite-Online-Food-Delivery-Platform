using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBite.Order.DTOs;
using QuickBite.Order.Entities;
using QuickBite.Order.Interfaces;
using System.Security.Claims;

namespace QuickBite.Order.Controllers
{
    [ApiController]
    [Route("api/v1/orders")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderDto dto)
        {
            try
            {
                var customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _orderService.PlaceOrderAsync(customerId, dto);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [Authorize(Roles = "DELIVERY_AGENT,ADMIN")]
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailable()
        {
            try
            {
                var all = (await _orderService.GetAllOrdersAsync()).ToList();
                var available = all.Where(o => (o.Status == "PLACED" || o.Status == "CONFIRMED" || o.Status == "PREPARING" || o.Status == "READY") && o.AgentId == null).ToList();
                return Ok(available);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _orderService.GetOrderByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [Authorize]
        [HttpGet("customer")]
        public async Task<IActionResult> GetCustomerHistory()
        {
            var customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _orderService.GetCustomerHistoryAsync(customerId);
            return Ok(result);
        }

        [Authorize(Roles = "OWNER,ADMIN")]
        [HttpGet("restaurant/{rId}/stats")]
        public async Task<IActionResult> GetRestaurantStats(Guid rId)
        {
            var result = await _orderService.GetRestaurantStatsAsync(rId);
            return Ok(result);
        }
        [HttpGet("restaurant/{rId}")]
        public async Task<IActionResult> GetRestaurantOrders(Guid rId)
        {
            var result = await _orderService.GetRestaurantOrdersAsync(rId);
            return Ok(result);
        }

        [Authorize(Roles = "DELIVERY_AGENT,ADMIN")]
        [HttpGet("agent")]
        public async Task<IActionResult> GetAgentOrders()
        {
            // Assuming AgentId is the same as UserId for simplicity or stored in Claims
            var agentId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _orderService.GetAgentOrdersAsync(agentId);
            return Ok(result);
        }

        [Authorize(Roles = "OWNER,DELIVERY_AGENT,ADMIN")]
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusDto dto)
        {
            var role = User.FindFirstValue(ClaimTypes.Role) ?? "USER";
            var result = await _orderService.UpdateStatusAsync(id, dto.NewStatus, role);
            return Ok(result);
        }

        [Authorize]
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(Guid id)
        {
            var customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _orderService.CancelOrderAsync(id, customerId);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("{id}/reorder")]
        public async Task<IActionResult> Reorder(Guid id)
        {
            var customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _orderService.ReorderAsync(id, customerId);
            return Ok(result);
        }

        [Authorize(Roles = "DELIVERY_AGENT,ADMIN")]
        [HttpPut("{id}/assign-agent")]
        public async Task<IActionResult> AssignAgent(Guid id, [FromQuery] Guid agentId)
        {
            var result = await _orderService.AssignAgentAsync(id, agentId);
            return Ok(result);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _orderService.GetAllOrdersAsync();
            return Ok(result);
        }

        [HttpPost("seed")]
        public async Task<IActionResult> SeedOrders([FromQuery] Guid restaurantId, [FromQuery] int count = 5)
        {
            try
            {
                await _orderService.SeedRestaurantOrdersAsync(restaurantId, count);
                return Ok(new { Message = $"{count} orders seeded for restaurant {restaurantId}" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
