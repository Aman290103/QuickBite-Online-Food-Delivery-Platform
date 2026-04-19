using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBite.Auth.DTOs;
using QuickBite.Auth.Interfaces;
using System.Security.Claims;

namespace QuickBite.Auth.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                var user = await _authService.RegisterAsync(registerDto);
                return CreatedAtAction(nameof(GetProfile), new { }, new { Message = "User registered successfully", UserId = user.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var tokens = await _authService.LoginAsync(loginDto);
                return Ok(tokens);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] TokenDto tokenDto)
        {
            try
            {
                var tokens = await _authService.RefreshTokenAsync(tokenDto.AccessToken, tokenDto.RefreshToken);
                return Ok(tokens);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var profile = await _authService.GetProfileAsync(userId);
            return Ok(profile);
        }

        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto updateDto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _authService.UpdateProfileAsync(userId, updateDto);
            return Ok(new { Message = "Profile updated successfully" });
        }

        [Authorize]
        [HttpPut("password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto passwordDto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _authService.ChangePasswordAsync(userId, passwordDto);
            return Ok(new { Message = "Password changed successfully" });
        }

        [Authorize]
        [HttpDelete("deactivate")]
        public async Task<IActionResult> DeactivateAccount()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _authService.DeactivateAccountAsync(userId);
            return Ok(new { Message = "Account deactivated successfully" });
        }

        // --- Admin Endpoints ---

        [Authorize(Roles = "ADMIN")]
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _authService.GetAllUsersAsync();
            return Ok(users);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPut("users/{id}/suspend")]
        public async Task<IActionResult> SuspendUser(Guid id, [FromQuery] bool suspend = true)
        {
            await _authService.SuspendUserAsync(id, suspend);
            return Ok(new { Message = $"User {(suspend ? "suspended" : "reactivated")} successfully" });
        }

        [Authorize(Roles = "ADMIN")]
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            await _authService.DeleteUserAsync(id);
            return Ok(new { Message = "User deleted successfully" });
        }
    }
}
