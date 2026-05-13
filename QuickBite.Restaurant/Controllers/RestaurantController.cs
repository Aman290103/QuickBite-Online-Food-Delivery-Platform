using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBite.Restaurant.DTOs;
using QuickBite.Restaurant.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Claims;

namespace QuickBite.Restaurant.Controllers
{
    [ApiController]
    [Route("api/v1/restaurants")]
    public class RestaurantController : ControllerBase
    {
        private readonly IRestaurantService _restaurantService;
        private readonly IConfiguration _configuration;
        private readonly IDistributedCache _cache;
        
        public RestaurantController(IRestaurantService restaurantService, IConfiguration configuration, IDistributedCache cache)
        {
            _restaurantService = restaurantService;
            _configuration = configuration;
            _cache = cache;
        }

        [Authorize(Roles = "OWNER")]
        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterRestaurantDto dto)
        {
            var ownerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _restaurantService.RegisterRestaurantAsync(ownerId, dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpDelete("mock-data")]
        public async Task<IActionResult> ClearMockData()
        {
            var all = await _restaurantService.GetNearbyRestaurantsAsync(0, 0, 100000); // Get all
            int count = 0;
            foreach (var r in all)
            {
                // Identify mock data by the "Name Index" pattern or "Automatically seeded" description
                if (System.Text.RegularExpressions.Regex.IsMatch(r.Name, @"\s\d+$") || r.Description.Contains("Automatically seeded"))
                {
                    await _restaurantService.DeleteRestaurantAsync(r.Id);
                    count++;
                }
            }
            return Ok(new { Message = $"Cleaned up {count} mock restaurants.", Count = count });
        }

        // Seeding and syncing endpoints removed as requested.

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _restaurantService.GetRestaurantByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpGet("nearby")]
        public async Task<IActionResult> GetNearby([FromQuery] double lat, [FromQuery] double lon, [FromQuery] double radius = 5.0)
        {
            var result = await _restaurantService.GetNearbyRestaurantsAsync(lat, lon, radius);
            return Ok(result);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string name)
        {
            var result = await _restaurantService.SearchRestaurantsAsync(name);
            return Ok(result);
        }

        [HttpGet("cuisine/{cuisine}")]
        public async Task<IActionResult> FilterByCuisine(string cuisine)
        {
            var result = await _restaurantService.GetRestaurantsByCuisineAsync(cuisine);
            return Ok(result);
        }

        [Authorize(Roles = "OWNER")]
        [HttpGet("my-restaurants")]
        public async Task<IActionResult> GetMyRestaurants()
        {
            var ownerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _restaurantService.GetRestaurantsByOwnerAsync(ownerId);
            return Ok(result);
        }

        [Authorize(Roles = "OWNER")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRestaurantDto dto)
        {
            var ownerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            try
            {
                var result = await _restaurantService.UpdateRestaurantAsync(id, ownerId, dto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }

        [Authorize(Roles = "ADMIN")]
        [HttpGet("admin-fetch-all")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _restaurantService.GetAllRestaurantsAsync();
            return Ok(result);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPending()
        {
            var result = await _restaurantService.GetPendingApprovalsAsync();
            return Ok(result);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> Approve(Guid id)
        {
            await _restaurantService.ApproveRestaurantAsync(id);
            return Ok(new { Message = "Restaurant approved successfully" });
        }

        [Authorize(Roles = "OWNER")]
        [HttpPut("{id}/toggle")]
        public async Task<IActionResult> Toggle(Guid id)
        {
            var ownerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            try
            {
                await _restaurantService.ToggleRestaurantStatusAsync(id, ownerId);
                return Ok(new { Message = "Restaurant status toggled" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }

        [Authorize(Roles = "ADMIN")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _restaurantService.DeleteRestaurantAsync(id);
            return Ok(new { Message = "Restaurant deleted" });
        }

        // --- Review Endpoints ---

        [HttpPost("{id}/reviews")]
        [Authorize]
        public async Task<IActionResult> SubmitReview(Guid id, [FromBody] AddReviewDto dto)
        {
            try
            {
                var customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _restaurantService.SubmitReviewAsync(id, customerId, dto);
                return CreatedAtAction(nameof(GetReviews), new { id = id }, result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
        }

        [HttpGet("{id}/reviews")]
        public async Task<IActionResult> GetReviews(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _restaurantService.GetReviewsAsync(id, page, pageSize);
            return Ok(result);
        }

        [HttpGet("{id}/reviews/avg")]
        public async Task<IActionResult> GetAverageRating(Guid id)
        {
            var result = await _restaurantService.GetAvgRatingAsync(id);
            return Ok(new { AverageRating = result });
        }

        [HttpDelete("reviews/{reviewId}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> DeleteReview(Guid reviewId)
        {
            await _restaurantService.DeleteReviewAsync(reviewId);
            return Ok(new { Message = "Review moderated/deleted" });
        }
    }
}
