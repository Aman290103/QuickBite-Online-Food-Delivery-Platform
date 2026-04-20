using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBite.Delivery.DTOs;
using QuickBite.Delivery.Interfaces;
using System.Security.Claims;

namespace QuickBite.Delivery.Controllers
{
    [ApiController]
    [Route("api/v1/agents")]
    public class DeliveryController : ControllerBase
    {
        private readonly IDeliveryService _deliveryService;

        public DeliveryController(IDeliveryService deliveryService)
        {
            _deliveryService = deliveryService;
        }

        [Authorize]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterAgentDto dto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _deliveryService.RegisterAgentAsync(userId, dto);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProfile(Guid id)
        {
            var result = await _deliveryService.GetProfileAsync(id);
            return Ok(result);
        }

        [HttpGet("nearby")]
        public async Task<IActionResult> GetNearbyAgents([FromQuery] double lat, [FromQuery] double lon, [FromQuery] double radius = 5)
        {
            var result = await _deliveryService.GetNearbyAgentsAsync(lat, lon, radius);
            return Ok(result);
        }

        [Authorize]
        [HttpPut("{id}/location")]
        public async Task<IActionResult> UpdateLocation(Guid id, [FromBody] UpdateLocationDto dto)
        {
            await _deliveryService.UpdateLocationAsync(id, dto);
            return NoContent();
        }

        [Authorize]
        [HttpPut("{id}/availability")]
        public async Task<IActionResult> ToggleAvailability(Guid id, [FromBody] ToggleAvailabilityDto dto)
        {
            await _deliveryService.SetAvailabilityAsync(id, dto.IsAvailable);
            return NoContent();
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPut("{id}/verify")]
        public async Task<IActionResult> VerifyAgent(Guid id)
        {
            await _deliveryService.VerifyAgentAsync(id);
            return NoContent();
        }

        [HttpPost("{id}/complete-delivery")]
        public async Task<IActionResult> CompleteDelivery(Guid id)
        {
            await _deliveryService.CompleteDeliveryAsync(id);
            return Ok(new { message = "Delivery completed successfully." });
        }
    }
}
