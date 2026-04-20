using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBite.Payment.DTOs;
using QuickBite.Payment.Interfaces;
using System.Security.Claims;

namespace QuickBite.Payment.Controllers
{
    [ApiController]
    [Route("api/v1/payments")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [Authorize]
        [HttpPost("process")]
        public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentDto dto)
        {
            var customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _paymentService.ProcessPaymentAsync(customerId, dto);
            return Ok(result);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPost("refund")]
        public async Task<IActionResult> Refund([FromQuery] Guid orderId)
        {
            var result = await _paymentService.RefundPaymentAsync(orderId);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("customer")]
        public async Task<IActionResult> GetCustomerHistory()
        {
            var customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _paymentService.GetCustomerPaymentsAsync(customerId);
            return Ok(result);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _paymentService.GetAllPaymentsAsync();
            return Ok(result);
        }
    }
}
