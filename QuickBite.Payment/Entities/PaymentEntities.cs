using System.ComponentModel.DataAnnotations;

namespace QuickBite.Payment.Entities
{
    public class Payment
    {
        [Key]
        public Guid PaymentId { get; set; }
        public Guid OrderId { get; set; }
        public Guid CustomerId { get; set; }
        
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.PENDING;
        public PaymentMode Mode { get; set; }
        
        public string? TransactionId { get; set; }
        public string Currency { get; set; } = "INR";
        
        public DateTime? PaidAt { get; set; }
        public DateTime? RefundedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Wallet
    {
        [Key]
        public Guid WalletId { get; set; }
        public Guid CustomerId { get; set; }
        public decimal Balance { get; set; } = 0;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class WalletStatement
    {
        [Key]
        public Guid StatementId { get; set; }
        public Guid WalletId { get; set; }
        
        public string Type { get; set; } = "CREDIT"; // CREDIT / DEBIT
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? TransactionRef { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
