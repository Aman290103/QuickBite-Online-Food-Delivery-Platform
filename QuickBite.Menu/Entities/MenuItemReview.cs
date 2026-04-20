using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuickBite.Menu.Entities
{
    public class MenuItemReview
    {
        [Key]
        public Guid ReviewId { get; set; }

        [Required]
        public Guid MenuItemId { get; set; }

        [Required]
        public Guid RestaurantId { get; set; } // Stored for quick filtering

        [Required]
        public Guid OrderId { get; set; }

        [Required]
        public Guid CustomerId { get; set; }

        [Required]
        [Range(1, 5)]
        public int ItemRating { get; set; }

        public string? Comment { get; set; }

        public DateTime ReviewDate { get; set; } = DateTime.UtcNow;

        public bool IsVerified { get; set; } = true;

        // Navigation
        [ForeignKey("MenuItemId")]
        public MenuItem MenuItem { get; set; } = null!;
    }
}
