using System.ComponentModel.DataAnnotations;

namespace QuickBite.Menu.DTOs
{
    public record AddCategoryDto(
        [Required] Guid RestaurantId,
        [Required] string Name,
        string Description,
        int DisplayOrder
    );

    public record AddMenuItemDto(
        [Required] Guid RestaurantId,
        [Required] Guid CategoryId,
        [Required] string Name,
        string Description,
        [Required] decimal Price,
        decimal? DiscountedPrice,
        bool IsVeg,
        int Calories,
        List<string> Tags
    );

    public record UpdateMenuItemDto(
        string Name,
        string Description,
        decimal Price,
        decimal? DiscountedPrice,
        bool IsVeg,
        int Calories,
        List<string> Tags
    );

    public record MenuItemResponseDto(
        Guid ItemId,
        Guid CategoryId,
        string Name,
        string Description,
        decimal Price,
        decimal? DiscountedPrice,
        string? ImageUrl,
        bool IsVeg,
        bool IsAvailable,
        double Rating,
        int Calories,
        List<string> Tags
    );

    public record MenuCategoryResponseDto(
        Guid CategoryId,
        string Name,
        string Description,
        string? ImageUrl,
        int DisplayOrder,
        List<MenuItemResponseDto> Items
    );

    public record MenuResponseDto(
        Guid RestaurantId,
        List<MenuCategoryResponseDto> Categories
    );

    public record SubmitMenuItemReviewDto(
        [Required] Guid OrderId,
        [Required][Range(1, 5)] int ItemRating,
        string? Comment
    );

    public record MenuItemReviewResponseDto(
        Guid ReviewId,
        Guid MenuItemId,
        Guid CustomerId,
        int ItemRating,
        string? Comment,
        DateTime ReviewDate
    );
}
