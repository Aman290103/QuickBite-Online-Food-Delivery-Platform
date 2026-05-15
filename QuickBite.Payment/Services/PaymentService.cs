using QuickBite.Payment.DTOs;
using QuickBite.Payment.Entities;
using QuickBite.Payment.Interfaces;

namespace QuickBite.Payment.Services
{
    // [SERVICE: PAYMENT & WALLET]
    // Handles financial transactions and internal wallet management.
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _repository;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(IPaymentRepository repository, ILogger<PaymentService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<PaymentDto> ProcessPaymentAsync(Guid customerId, ProcessPaymentDto dto)
        {
            var payment = new Entities.Payment { 
                PaymentId = Guid.NewGuid(), 
                OrderId = dto.OrderId, 
                CustomerId = customerId, 
                Amount = dto.Amount, 
                Status = PaymentStatus.PAID,
                Mode = dto.Mode,
                CreatedAt = DateTime.UtcNow
            };
            await _repository.AddPaymentAsync(payment);
            return MapToDto(payment);
        }

        public async Task<PaymentDto> RefundPaymentAsync(Guid orderId)
        {
            var p = await _repository.GetPaymentByOrderIdAsync(orderId);
            if (p != null) { 
                p.Status = PaymentStatus.REFUNDED; 
                p.RefundedAt = DateTime.UtcNow;
                await _repository.UpdatePaymentAsync(p); 
            }
            return MapToDto(p!);
        }

        public async Task<IEnumerable<PaymentDto>> GetCustomerPaymentsAsync(Guid customerId)
        {
            var payments = await _repository.GetAllPaymentsAsync();
            return payments.Where(p => p.CustomerId == customerId).Select(MapToDto);
        }

        public async Task<IEnumerable<PaymentDto>> GetAllPaymentsAsync()
        {
            var payments = await _repository.GetAllPaymentsAsync();
            return payments.Select(MapToDto);
        }

        public async Task<WalletResponseDto> GetWalletBalanceAsync(Guid customerId)
        {
            var wallet = await _repository.GetWalletByCustomerIdAsync(customerId);
            if (wallet == null) {
                wallet = new Wallet { WalletId = Guid.NewGuid(), CustomerId = customerId, Balance = 10000 };
                await _repository.AddWalletAsync(wallet);
            }
            return new WalletResponseDto(wallet.CustomerId, wallet.Balance);
        }

        public async Task<WalletResponseDto> AddMoneyToWalletAsync(Guid customerId, AddToWalletDto dto)
        {
            var wallet = await _repository.GetWalletByCustomerIdAsync(customerId);
            if (wallet != null) { 
                wallet.Balance += dto.Amount; 
                wallet.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateWalletAsync(wallet); 
            }
            return new WalletResponseDto(customerId, wallet?.Balance ?? 0);
        }

        public async Task<IEnumerable<WalletStatementDto>> GetWalletStatementsAsync(Guid customerId)
        {
            // Implementation for fetching statements if the repo supports it
            await Task.CompletedTask;
            return new List<WalletStatementDto>();
        }

        public async Task<string> CreateRazorpayOrderAsync(decimal amount, string receipt)
        {
            await Task.CompletedTask;
            return "rzp_test_order_id";
        }

        private PaymentDto MapToDto(Entities.Payment p) => new PaymentDto(
            p.PaymentId, 
            p.OrderId, 
            p.Amount, 
            p.Status, 
            p.Mode, 
            p.TransactionId, 
            p.CreatedAt
        );
    }
}
