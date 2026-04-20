using Microsoft.EntityFrameworkCore.Storage;
using QuickBite.Payment.Entities;

namespace QuickBite.Payment.Interfaces
{
    public interface IPaymentRepository
    {
        Task<Entities.Payment?> GetPaymentByOrderIdAsync(Guid orderId);
        Task<Entities.Payment?> GetPaymentByIdAsync(Guid paymentId);
        Task<IEnumerable<Entities.Payment>> GetPaymentsByCustomerIdAsync(Guid customerId);
        Task<IEnumerable<Entities.Payment>> GetAllPaymentsAsync();
        
        Task AddPaymentAsync(Entities.Payment payment);
        Task UpdatePaymentAsync(Entities.Payment payment);
        
        Task<Wallet?> GetWalletByCustomerIdAsync(Guid customerId);
        Task AddWalletAsync(Wallet wallet);
        Task UpdateWalletAsync(Wallet wallet);
        
        Task AddWalletStatementAsync(WalletStatement statement);
        Task<IEnumerable<WalletStatement>> GetWalletStatementsAsync(Guid walletId);
        
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task SaveChangesAsync();
    }
}
