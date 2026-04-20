namespace QuickBite.Order.Events
{
    public interface OrderPlacedEvent
    {
        Guid OrderId { get; }
        Guid CustomerId { get; }
        Guid RestaurantId { get; }
        decimal TotalAmount { get; }
    }
}
