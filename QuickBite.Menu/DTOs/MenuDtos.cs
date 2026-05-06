using System.ComponentModel.DataAnnotations;

namespace QuickBite.Menu.DTOs
{
    public record AddCategoryDto(
        [Required] Guid RestaurantId,
        [Required] string Name,
        string Description,
        int DisplayOrder
    );

    public class AddMenuItemDto
    {
        [Required] public Guid RestaurantId { get; set; }
        [Required] public Guid CategoryId { get; set; }
        [Required] public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        [Required] public decimal Price { get; set; }
        public decimal? DiscountedPrice { get; set; }
        public bool IsVeg { get; set; }
        public int Calories { get; set; }
        public string? ImageUrl { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    public record UpdateMenuItemDto(
        string Name,
        string Description,
        decimal Price,
        decimal? DiscountedPrice,
        bool IsVeg,
        int Calories,
        string? ImageUrl,
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
