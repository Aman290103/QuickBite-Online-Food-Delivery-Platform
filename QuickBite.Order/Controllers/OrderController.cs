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
            var customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _orderService.PlaceOrderAsync(customerId, dto);
            return CreatedAtAction(nameof(GetById), new { id = result.OrderId }, result);
        }

        [Authorize]
        [HttpGet("{id}")]
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
        [HttpGet("restaurant/{rId}")]
        public async Task<IActionResult> GetRestaurantOrders(Guid rId)
        {
            var result = await _orderService.GetRestaurantOrdersAsync(rId);
            return Ok(result);
        }

        [Authorize(Roles = "OWNER,AGENT,ADMIN")]
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

        [Authorize(Roles = "ADMIN")]
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
    }
}
