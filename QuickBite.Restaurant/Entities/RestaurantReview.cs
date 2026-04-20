using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuickBite.Restaurant.Entities
{
    public class RestaurantReview
    {
        [Key]
        public Guid ReviewId { get; set; }

        [Required]
        public Guid RestaurantId { get; set; }

        [Required]
        public Guid OrderId { get; set; } // Unique per restaurant review

        [Required]
        public Guid CustomerId { get; set; }

        [Required]
        [Range(1, 5)]
        public int FoodRating { get; set; }

        public string Comment { get; set; } = string.Empty;

        public DateTime ReviewDate { get; set; } = DateTime.UtcNow;

        public bool IsVerified { get; set; } = true;

        // Navigation
        [ForeignKey("RestaurantId")]
        public Restaurant Restaurant { get; set; } = null!;
    }
}
