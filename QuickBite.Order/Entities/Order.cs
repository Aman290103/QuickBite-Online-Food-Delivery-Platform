using System.ComponentModel.DataAnnotations;

namespace QuickBite.Order.Entities
{
    public class Order
    {
        [Key]
        public Guid OrderId { get; set; }
        public Guid CustomerId { get; set; }
        public Guid RestaurantId { get; set; }
        public Guid? DeliveryAgentId { get; set; }
        
        public decimal TotalAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal FinalAmount { get; set; }
        
        public string ModeOfPayment { get; set; } = "COD"; // COD or ONLINE
        public OrderStatus Status { get; set; } = OrderStatus.PLACED;
        
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public DateTime? EstimatedDelivery { get; set; }
        
        [Required]
        public string DeliveryAddress { get; set; } = string.Empty;
        public string? SpecialInstructions { get; set; }
        
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
