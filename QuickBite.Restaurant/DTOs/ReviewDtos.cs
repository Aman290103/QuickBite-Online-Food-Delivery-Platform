using System.ComponentModel.DataAnnotations;

namespace QuickBite.Restaurant.DTOs
{
    public record AddReviewDto(
        [Required] Guid OrderId,
        [Required][Range(1, 5)] int FoodRating,
        string Comment
    );

    public record ReviewResponseDto(
        Guid ReviewId,
        Guid RestaurantId,
        Guid OrderId,
        Guid CustomerId,
        string CustomerName,
        int FoodRating,
        string Comment,
        DateTime ReviewDate
    );
}
