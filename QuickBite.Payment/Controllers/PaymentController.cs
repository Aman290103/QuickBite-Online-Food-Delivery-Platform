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
            try
            {
                var customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _paymentService.ProcessPaymentAsync(customerId, dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> InternalProcessPayment([FromBody] dynamic dto)
        {
            // Internal call from Order Service to initiate a payment record
            // For now, we just return Ok to let the order be created.
            // The actual Razorpay verification happens later via the /process endpoint.
            return Ok();
        }

        [Authorize]
        [HttpPost("create-order")]
        public async Task<IActionResult> CreateOrder([FromQuery] decimal amount)
        {
            var receipt = Guid.NewGuid().ToString();
            var orderId = await _paymentService.CreateRazorpayOrderAsync(amount, receipt);
            return Ok(new { orderId });
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
