using System.ComponentModel.DataAnnotations;

namespace QuickBite.Cart.DTOs
{
    public record AddToCartDto(
        [Required] Guid RestaurantId,
        [Required] string RestaurantName,
        [Required] Guid MenuItemId,
        [Required] string Name,
        [Required] decimal Price,
        [Required][Range(1, 100)] int Quantity,
        string? Customization
    );

    public record UpdateQtyDto(
        [Required][Range(0, 100)] int Quantity
    );

    public record ApplyPromoDto(
        [Required] string PromoCode
    );

    public record CartItemResponseDto(
        Guid ItemId,
        Guid MenuItemId,
        string Name,
        decimal Price,
        int Quantity,
        decimal Total,
        string? Customization
    );

    public record CartResponseDto(
        Guid CartId,
        Guid RestaurantId,
        string RestaurantName,
        List<CartItemResponseDto> Items,
        decimal SubTotal,
        decimal DiscountAmount,
        decimal TaxAmount,
        string? AppliedPromoCode,
        decimal GrandTotal
    );
}
