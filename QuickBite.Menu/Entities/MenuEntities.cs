using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuickBite.Menu.Entities
{
    public class MenuCategory
    {
        [Key]
        public Guid CategoryId { get; set; }
        
        [Required]
        public Guid RestaurantId { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int DisplayOrder { get; set; } = 0;
        
        // Navigation Property
        public List<MenuItem> Items { get; set; } = new();
    }

    public class MenuItem
    {
        [Key]
        public Guid ItemId { get; set; }
        
        [Required]
        public Guid RestaurantId { get; set; }
        
        [Required]
        public Guid CategoryId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountedPrice { get; set; }
        
        public string? ImageUrl { get; set; }
        public bool IsVeg { get; set; } = true;
        public bool IsAvailable { get; set; } = true;
        public double Rating { get; set; } = 0.0;
        public int Calories { get; set; }
        public string Tags { get; set; } = string.Empty; // Comma-separated tags
        
        // Navigation Property
        [ForeignKey("CategoryId")]
        public MenuCategory Category { get; set; } = null!;
    }
}
