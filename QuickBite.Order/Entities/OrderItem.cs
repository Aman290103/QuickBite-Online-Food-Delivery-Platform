using System.ComponentModel.DataAnnotations;

namespace QuickBite.Order.Entities
{
    public class OrderItem
    {
        [Key]
        public Guid OrderItemId { get; set; }
        public Guid OrderId { get; set; }
        public Guid MenuItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? Customization { get; set; }

        public Order Order { get; set; } = null!;
    }
}
