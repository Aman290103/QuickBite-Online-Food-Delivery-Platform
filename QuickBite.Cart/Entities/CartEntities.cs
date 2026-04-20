using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuickBite.Cart.Entities
{
    public class Cart
    {
        [Key]
        public Guid CartId { get; set; }
        
        [Required]
        public Guid CustomerId { get; set; }
        
        [Required]
        public Guid RestaurantId { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; } = 0;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal GrandTotal { get; set; } = 0;
        
        public string? AppliedPromoCode { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation
        public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    }

    public class CartItem
    {
        [Key]
        public Guid ItemId { get; set; }
        
        [Required]
        public Guid CartId { get; set; }
        
        [Required]
        public Guid MenuItemId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // Snapshot
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; } // Snapshot
        
        [Required]
        public int Quantity { get; set; }
        
        public string? Customization { get; set; }
        
        [ForeignKey("CartId")]
        public Cart Cart { get; set; } = null!;
    }

    public enum DiscountType { PERCENT, FLAT }

    public class PromoCode
    {
        [Key]
        public Guid PromoId { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;
        
        [Required]
        public DiscountType DiscountType { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Value { get; set; }
        
        public bool IsActive { get; set; } = true;
        public DateTime ExpiresAt { get; set; }
    }
}
