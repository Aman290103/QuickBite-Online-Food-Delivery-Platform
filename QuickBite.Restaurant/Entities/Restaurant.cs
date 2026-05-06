using System.ComponentModel.DataAnnotations;

namespace QuickBite.Restaurant.Entities
{
    public class Restaurant
    {
        [Key]
        public Guid RestaurantId { get; set; }
        
        [Required]
        public Guid OwnerId { get; set; }
        
        public string? PlaceId { get; set; } // Google Place ID for deduplication
        
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public string Cuisine { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;
        
        [Required]
        public double Latitude { get; set; }
        
        [Required]
        public double Longitude { get; set; }
        
        [Phone]
        [MaxLength(100)]
        public string Phone { get; set; } = string.Empty;
        
        public double AvgRating { get; set; } = 0.0;
        public int ReviewCount { get; set; } = 0;
        
        public bool IsOpen { get; set; } = false;
        
        public bool IsApproved { get; set; } = false;
        
        public double DeliveryRadiusKm { get; set; } = 5.0;
        
        public decimal MinOrderAmount { get; set; } = 0;
        
        public int EstimatedDeliveryMin { get; set; } = 30;
        
        [MaxLength(1000)]
        public string? ImageUrl { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<RestaurantReview> Reviews { get; set; } = new List<RestaurantReview>();
    }
}
