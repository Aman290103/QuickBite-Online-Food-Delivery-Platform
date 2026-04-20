using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using QuickBite.Payment.Data;
using QuickBite.Payment.Entities;
using QuickBite.Payment.Interfaces;

namespace QuickBite.Payment.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly PaymentDbContext _context;

        public PaymentRepository(PaymentDbContext context)
        {
            _context = context;
        }

        public async Task<Entities.Payment?> GetPaymentByOrderIdAsync(Guid orderId)
        {
            return await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);
        }

        public async Task<Entities.Payment?> GetPaymentByIdAsync(Guid paymentId)
        {
            return await _context.Payments.FindAsync(paymentId);
        }

        public async Task<IEnumerable<Entities.Payment>> GetPaymentsByCustomerIdAsync(Guid customerId)
        {
            return await _context.Payments
                .Where(p => p.CustomerId == customerId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Entities.Payment>> GetAllPaymentsAsync()
        {
            return await _context.Payments
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task AddPaymentAsync(Entities.Payment payment)
        {
            await _context.Payments.AddAsync(payment);
        }

        public async Task UpdatePaymentAsync(Entities.Payment payment)
        {
            _context.Payments.Update(payment);
            await Task.CompletedTask;
        }

        public async Task<Wallet?> GetWalletByCustomerIdAsync(Guid customerId)
        {
            return await _context.Wallets.FirstOrDefaultAsync(w => w.CustomerId == customerId);
        }

        public async Task AddWalletAsync(Wallet wallet)
        {
            await _context.Wallets.AddAsync(wallet);
        }

        public async Task UpdateWalletAsync(Wallet wallet)
        {
            _context.Wallets.Update(wallet);
            await Task.CompletedTask;
        }

        public async Task AddWalletStatementAsync(WalletStatement statement)
        {
            await _context.WalletStatements.AddAsync(statement);
        }

        public async Task<IEnumerable<WalletStatement>> GetWalletStatementsAsync(Guid walletId)
        {
            return await _context.WalletStatements
                .Where(s => s.WalletId == walletId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
