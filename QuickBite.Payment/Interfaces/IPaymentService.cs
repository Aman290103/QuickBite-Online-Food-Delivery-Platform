using QuickBite.Payment.DTOs;
using QuickBite.Payment.Entities;

namespace QuickBite.Payment.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentDto> ProcessPaymentAsync(Guid customerId, ProcessPaymentDto dto);
        Task<PaymentDto> RefundPaymentAsync(Guid orderId);
        Task<IEnumerable<PaymentDto>> GetCustomerPaymentsAsync(Guid customerId);
        Task<IEnumerable<PaymentDto>> GetAllPaymentsAsync();
        
        Task<WalletResponseDto> GetWalletBalanceAsync(Guid customerId);
        Task<WalletResponseDto> AddMoneyToWalletAsync(Guid customerId, AddToWalletDto dto);
        Task<IEnumerable<WalletStatementDto>> GetWalletStatementsAsync(Guid customerId);
    }
}
