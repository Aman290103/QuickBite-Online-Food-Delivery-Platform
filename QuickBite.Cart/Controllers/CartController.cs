using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBite.Cart.DTOs;
using QuickBite.Cart.Interfaces;
using System.Security.Claims;

namespace QuickBite.Cart.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/cart")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _cartService.GetCartAsync(customerId);
            return Ok(result);
        }

        [HttpPost("items")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            try
            {
                var customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _cartService.AddToCartAsync(customerId, dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
        }

        [HttpPut("items/{itemId}/qty")]
        public async Task<IActionResult> UpdateQuantity(Guid itemId, [FromBody] UpdateQtyDto dto)
        {
            var customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _cartService.UpdateQuantityAsync(customerId, itemId, dto.Quantity);
            return Ok(result);
        }

        [HttpDelete("items/{itemId}")]
        public async Task<IActionResult> RemoveItem(Guid itemId)
        {
            var customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _cartService.RemoveItemAsync(customerId, itemId);
            return Ok(result);
        }

        [HttpDelete]
        public async Task<IActionResult> ClearCart()
        {
            var customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _cartService.ClearCartAsync(customerId);
            return Ok(new { Message = "Cart cleared" });
        }

        [HttpPost("promo")]
        public async Task<IActionResult> ApplyPromo([FromBody] ApplyPromoDto dto)
        {
            try
            {
                var customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _cartService.ApplyPromoCodeAsync(customerId, dto.PromoCode);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("switch-restaurant")]
        public async Task<IActionResult> SwitchRestaurant([FromQuery] Guid restaurantId)
        {
            var customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _cartService.ClearAndSwitchRestaurantAsync(customerId, restaurantId);
            return Ok(new { Message = "Cart cleared and switched to new restaurant" });
        }
    }
}
