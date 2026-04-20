using QuickBite.Payment.Entities;
using System.ComponentModel.DataAnnotations;

namespace QuickBite.Payment.DTOs
{
    public record ProcessPaymentDto(
        [Required] Guid OrderId,
        [Required] decimal Amount,
        [Required] PaymentMode Mode, // CARD, UPI, WALLET, COD
        string? RazorpayPaymentId,
        string? RazorpayOrderId,
        string? RazorpaySignature
    );

    public record WalletResponseDto(
        Guid CustomerId,
        decimal Balance
    );

    public record AddToWalletDto(
        [Required] decimal Amount,
        [Required] string RazorpayPaymentId
    );

    public record WalletStatementDto(
        Guid StatementId,
        string Type,
        decimal Amount,
        string Description,
        string? TransactionRef,
        DateTime CreatedAt
    );

    public record PaymentDto(
        Guid PaymentId,
        Guid OrderId,
        decimal Amount,
        PaymentStatus Status,
        PaymentMode Mode,
        string? TransactionId,
        DateTime CreatedAt
    );
}
