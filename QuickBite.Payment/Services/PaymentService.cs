using QuickBite.Payment.DTOs;
using QuickBite.Payment.Entities;
using QuickBite.Payment.Gateways;
using QuickBite.Payment.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace QuickBite.Payment.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _repository;
        private readonly RazorpayGateway _razorpay;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(IPaymentRepository repository, RazorpayGateway razorpay, ILogger<PaymentService> logger)
        {
            _repository = repository;
            _razorpay = razorpay;
            _logger = logger;
        }

        public async Task<PaymentDto> ProcessPaymentAsync(Guid customerId, ProcessPaymentDto dto)
        {
            _logger.LogInformation("Processing {Mode} payment of {Amount} for Order {OrderId}", dto.Mode, dto.Amount, dto.OrderId);

            var payment = new Entities.Payment
            {
                PaymentId = Guid.NewGuid(),
                OrderId = dto.OrderId,
                CustomerId = customerId,
                Amount = dto.Amount,
                Mode = dto.Mode,
                Status = PaymentStatus.PENDING
            };

            switch (dto.Mode)
            {
                case PaymentMode.COD:
                    payment.Status = PaymentStatus.PENDING; // Pay on delivery
                    break;

                case PaymentMode.CARD:
                case PaymentMode.UPI:
                    // EMERGENCY BYPASS: Always succeed for mock demo IDs
                    if (dto.RazorpayPaymentId?.StartsWith("pay_mock_") == true || dto.RazorpaySignature == "sig_mock_verified")
                    {
                        payment.Status = PaymentStatus.PAID;
                        payment.TransactionId = dto.RazorpayPaymentId ?? "mock_id";
                        payment.PaidAt = DateTime.UtcNow;
                        await _repository.AddPaymentAsync(payment);
                        await _repository.SaveChangesAsync();
                        return MapToDto(payment);
                    }

                    if (string.IsNullOrEmpty(dto.RazorpayPaymentId) || string.IsNullOrEmpty(dto.RazorpayOrderId) || string.IsNullOrEmpty(dto.RazorpaySignature))
                        throw new Exception("Razorpay details missing.");
                    
                    bool isValid = _razorpay.VerifySignature(dto.RazorpayPaymentId, dto.RazorpayOrderId, dto.RazorpaySignature);
                    if (!isValid)
                    {
                        payment.Status = PaymentStatus.FAILED;
                        await _repository.AddPaymentAsync(payment);
                        await _repository.SaveChangesAsync();
                        throw new Exception("Payment signature verification failed.");
                    }
                    payment.Status = PaymentStatus.PAID;
                    payment.TransactionId = dto.RazorpayPaymentId;
                    payment.PaidAt = DateTime.UtcNow;
                    break;

                case PaymentMode.WALLET:
                    var strategy = _repository.CreateExecutionStrategy();
                    await strategy.ExecuteAsync<bool>(async () =>
                    {
                        using (var transaction = await _repository.BeginTransactionAsync())
                        {
                            try
                            {
                                var wallet = await _repository.GetWalletByCustomerIdAsync(customerId);
                                if (wallet == null || wallet.Balance < dto.Amount)
                                    throw new InvalidOperationException("Insufficient wallet balance.");

                                wallet.Balance -= dto.Amount;
                                wallet.UpdatedAt = DateTime.UtcNow;
                                await _repository.UpdateWalletAsync(wallet);

                                await _repository.AddWalletStatementAsync(new WalletStatement
                                {
                                    StatementId = Guid.NewGuid(),
                                    WalletId = wallet.WalletId,
                                    Type = "DEBIT",
                                    Amount = dto.Amount,
                                    Description = $"Payment for Order {dto.OrderId}",
                                    TransactionRef = payment.PaymentId.ToString()
                                });

                                payment.Status = PaymentStatus.PAID;
                                payment.PaidAt = DateTime.UtcNow;
                                await _repository.AddPaymentAsync(payment);

                                await _repository.SaveChangesAsync();
                                await transaction.CommitAsync();
                                return true;
                            }
                            catch (Exception ex)
                            {
                                await transaction.RollbackAsync();
                                _logger.LogError(ex, "Wallet payment failed for Order {OrderId}", dto.OrderId);
                                throw;
                            }
                        }
                    });
                    return MapToDto(payment);
            }

            await _repository.AddPaymentAsync(payment);
            await _repository.SaveChangesAsync();

            return MapToDto(payment);
        }

        public async Task<PaymentDto> RefundPaymentAsync(Guid orderId)
        {
            var payment = await _repository.GetPaymentByOrderIdAsync(orderId);
            if (payment == null) throw new Exception("Payment record not found.");
            if (payment.Status != PaymentStatus.PAID) throw new Exception("Only paid orders can be refunded.");

            _logger.LogInformation("Initiating refund for Order {OrderId} (Amount: {Amount})", orderId, payment.Amount);

            if (payment.Mode == PaymentMode.WALLET)
            {
                var strategy = _repository.CreateExecutionStrategy();
                await strategy.ExecuteAsync<bool>(async () =>
                {
                    using (var transaction = await _repository.BeginTransactionAsync())
                    {
                        try
                        {
                            var wallet = await _repository.GetWalletByCustomerIdAsync(payment.CustomerId);
                            if (wallet != null)
                            {
                                wallet.Balance += payment.Amount;
                                await _repository.UpdateWalletAsync(wallet);
                                await _repository.AddWalletStatementAsync(new WalletStatement
                                {
                                    StatementId = Guid.NewGuid(),
                                    WalletId = wallet.WalletId,
                                    Type = "CREDIT",
                                    Amount = payment.Amount,
                                    Description = $"Refund for Order {orderId}",
                                    TransactionRef = payment.PaymentId.ToString()
                                });
                            }
                            payment.Status = PaymentStatus.REFUNDED;
                            payment.RefundedAt = DateTime.UtcNow;
                            await _repository.UpdatePaymentAsync(payment);
                            await _repository.SaveChangesAsync();
                            await transaction.CommitAsync();
                            return true;
                        }
                        catch (Exception)
                        {
                            await transaction.RollbackAsync();
                            throw;
                        }
                    }
                });
            }
            else if (payment.Mode == PaymentMode.CARD || payment.Mode == PaymentMode.UPI)
            {
                string refundId = _razorpay.Refund(payment.TransactionId!, payment.Amount);
                payment.Status = PaymentStatus.REFUNDED;
                payment.RefundedAt = DateTime.UtcNow;
                payment.TransactionId += $"_REF_{refundId}";
                await _repository.UpdatePaymentAsync(payment);
                await _repository.SaveChangesAsync();
            }
            else
            {
                payment.Status = PaymentStatus.REFUNDED; // COD refund manual
                await _repository.UpdatePaymentAsync(payment);
                await _repository.SaveChangesAsync();
            }

            return MapToDto(payment);
        }

        public async Task<WalletResponseDto> GetWalletBalanceAsync(Guid customerId)
        {
            var wallet = await GetOrCreateWallet(customerId);
            return new WalletResponseDto(customerId, wallet.Balance);
        }

        public async Task<WalletResponseDto> AddMoneyToWalletAsync(Guid customerId, AddToWalletDto dto)
        {
            // Note: In real setup, verify Razorpay payment before adding balance
            var wallet = await GetOrCreateWallet(customerId);
            wallet.Balance += dto.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            await _repository.AddWalletStatementAsync(new WalletStatement
            {
                StatementId = Guid.NewGuid(),
                WalletId = wallet.WalletId,
                Type = "CREDIT",
                Amount = dto.Amount,
                Description = "Wallet Top-up",
                TransactionRef = dto.RazorpayPaymentId
            });

            await _repository.UpdateWalletAsync(wallet);
            await _repository.SaveChangesAsync();

            return new WalletResponseDto(customerId, wallet.Balance);
        }

        public async Task<IEnumerable<WalletStatementDto>> GetWalletStatementsAsync(Guid customerId)
        {
            var wallet = await GetOrCreateWallet(customerId);
            var statements = await _repository.GetWalletStatementsAsync(wallet.WalletId);
            return statements.Select(s => new WalletStatementDto(s.StatementId, s.Type, s.Amount, s.Description, s.TransactionRef, s.CreatedAt));
        }

        public async Task<IEnumerable<PaymentDto>> GetCustomerPaymentsAsync(Guid customerId)
        {
            var payments = await _repository.GetPaymentsByCustomerIdAsync(customerId);
            return payments.Select(MapToDto);
        }

        public async Task<IEnumerable<PaymentDto>> GetAllPaymentsAsync()
        {
            var payments = await _repository.GetAllPaymentsAsync();
            return payments.Select(MapToDto);
        }

        public async Task<string> CreateRazorpayOrderAsync(decimal amount, string receipt)
        {
            return await Task.Run(() => _razorpay.CreateOrder(amount, receipt));
        }

        private async Task<Wallet> GetOrCreateWallet(Guid customerId)
        {
            var wallet = await _repository.GetWalletByCustomerIdAsync(customerId);
            if (wallet == null)
            {
                // SEEDING: Give 10,000 INR to new users automatically for testing
                wallet = new Wallet { WalletId = Guid.NewGuid(), CustomerId = customerId, Balance = 10000 };
                await _repository.AddWalletAsync(wallet);
                await _repository.SaveChangesAsync();

                await _repository.AddWalletStatementAsync(new WalletStatement
                {
                    StatementId = Guid.NewGuid(),
                    WalletId = wallet.WalletId,
                    Type = "CREDIT",
                    Amount = 10000,
                    Description = "Welcome Bonus",
                    TransactionRef = "WELCOME_2026"
                });
                await _repository.SaveChangesAsync();
            }
            return wallet;
        }

        private PaymentDto MapToDto(Entities.Payment p) => new PaymentDto(
            p.PaymentId, p.OrderId, p.Amount, p.Status, p.Mode, p.TransactionId, p.CreatedAt
        );
    }
}
