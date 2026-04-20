using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBite.Payment.DTOs;
using QuickBite.Payment.Interfaces;
using System.Security.Claims;

namespace QuickBite.Payment.Controllers
{
    [ApiController]
    [Route("api/v1/wallet")]
    public class WalletController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public WalletController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [Authorize]
        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance()
        {
            var customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _paymentService.GetWalletBalanceAsync(customerId);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("add")]
        public async Task<IActionResult> AddMoney([FromBody] AddToWalletDto dto)
        {
            var customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _paymentService.AddMoneyToWalletAsync(customerId, dto);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("statements")]
        public async Task<IActionResult> GetStatements()
        {
            var customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _paymentService.GetWalletStatementsAsync(customerId);
            return Ok(result);
        }
    }
}
