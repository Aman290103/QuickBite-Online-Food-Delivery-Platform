using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBite.Menu.DTOs;
using QuickBite.Menu.Interfaces;
using System.Security.Claims;

namespace QuickBite.Menu.Controllers
{
    [ApiController]
    [Route("api/v1/menu")]
    public class MenuController : ControllerBase
    {
        private readonly IMenuService _menuService;

        public MenuController(IMenuService menuService)
        {
            _menuService = menuService;
        }

        [HttpGet("{restaurantId}")]
        public async Task<IActionResult> GetMenu(Guid restaurantId)
        {
            var result = await _menuService.GetMenuAsync(restaurantId);
            return Ok(result);
        }

        [HttpGet("{restaurantId}/search")]
        public async Task<IActionResult> Search(Guid restaurantId, [FromQuery] string name)
        {
            var result = await _menuService.SearchItemsAsync(restaurantId, name);
            return Ok(result);
        }

        [HttpGet("{restaurantId}/veg")]
        public async Task<IActionResult> GetVegItems(Guid restaurantId)
        {
            var result = await _menuService.GetVegItemsAsync(restaurantId);
            return Ok(result);
        }

        [Authorize(Roles = "OWNER")]
        [HttpPost("categories")]
        public async Task<IActionResult> AddCategory([FromBody] AddCategoryDto dto)
        {
            var ownerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _menuService.AddCategoryAsync(ownerId, dto);
            return CreatedAtAction(nameof(GetMenu), new { restaurantId = dto.RestaurantId }, result);
        }

        [Authorize(Roles = "OWNER")]
        [HttpPut("categories/{id}")]
        public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] AddCategoryDto dto)
        {
            var ownerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _menuService.UpdateCategoryAsync(ownerId, id, dto);
            return Ok(new { Message = "Category updated" });
        }

        [Authorize(Roles = "OWNER")]
        [HttpDelete("categories/{id}")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            var ownerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _menuService.DeleteCategoryAsync(ownerId, id);
            return Ok(new { Message = "Category deleted" });
        }

        [Authorize(Roles = "OWNER")]
        [HttpPost("items")]
        public async Task<IActionResult> AddMenuItem([FromBody] AddMenuItemDto dto)
        {
            var ownerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _menuService.AddMenuItemAsync(ownerId, dto);
            return CreatedAtAction(nameof(GetMenu), new { restaurantId = dto.RestaurantId }, result);
        }

        [Authorize(Roles = "OWNER")]
        [HttpPut("items/{id}")]
        public async Task<IActionResult> UpdateMenuItem(Guid id, [FromBody] UpdateMenuItemDto dto)
        {
            var ownerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _menuService.UpdateMenuItemAsync(ownerId, id, dto);
            return Ok(result);
        }

        [Authorize(Roles = "OWNER")]
        [HttpPut("items/{id}/toggle")]
        public async Task<IActionResult> ToggleAvailability(Guid id)
        {
            var ownerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _menuService.ToggleItemAvailabilityAsync(ownerId, id);
            return Ok(new { Message = "Availability toggled" });
        }

        [Authorize(Roles = "OWNER")]
        [HttpDelete("items/{id}")]
        public async Task<IActionResult> DeleteMenuItem(Guid id)
        {
            var ownerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _menuService.DeleteMenuItemAsync(ownerId, id);
            return Ok(new { Message = "Item deleted" });
        }

        // --- Item Review Endpoints ---

        [Authorize]
        [HttpPost("items/{itemId}/reviews")]
        public async Task<IActionResult> SubmitReview(Guid itemId, [FromBody] SubmitMenuItemReviewDto dto)
        {
            var customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _menuService.SubmitItemReviewAsync(itemId, customerId, dto);
            return CreatedAtAction(nameof(GetItemReviews), new { itemId = itemId }, result);
        }

        [HttpGet("items/{itemId}/reviews")]
        public async Task<IActionResult> GetItemReviews(Guid itemId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _menuService.GetItemReviewsAsync(itemId, page, pageSize);
            return Ok(result);
        }

        [HttpGet("items/{itemId}/reviews/avg")]
        public async Task<IActionResult> GetAvgRating(Guid itemId)
        {
            var result = await _menuService.GetAvgItemRatingAsync(itemId);
            return Ok(new { AverageRating = result });
        }

        [Authorize(Roles = "ADMIN")]
        [HttpDelete("items/reviews/{reviewId}")]
        public async Task<IActionResult> ModerateReview(Guid reviewId)
        {
            await _menuService.ModerateItemReviewAsync(reviewId);
            return Ok(new { Message = "Review moderated/deleted" });
        }
    }
}
