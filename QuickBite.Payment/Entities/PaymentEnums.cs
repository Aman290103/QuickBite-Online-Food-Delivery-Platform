namespace QuickBite.Payment.Entities
{
    public enum PaymentStatus
    {
        PENDING,
        PAID,
        REFUNDED,
        FAILED
    }

    public enum PaymentMode
    {
        CARD,
        UPI,
        WALLET,
        COD
    }
}
