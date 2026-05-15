using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using QuickBite.Auth.DTOs;
using QuickBite.Auth.Entities;
using QuickBite.Auth.Interfaces;

namespace QuickBite.Auth.Services
{
    // [SERVICE: AUTHENTICATION]
    // This service is responsible for all security and identity management within QuickBite.
    // It handles user registration, secure login with password hashing, and JWT token issuance.
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;

        public AuthService(
            UserManager<User> userManager,
            IUserRepository userRepository,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _userRepository = userRepository;
            _configuration = configuration;
        }

        // [METHOD: REGISTER]
        // Creates a new user in the system using ASP.NET Core Identity.
        // It validates email uniqueness and assigns roles (Customer, Owner, etc.).
        public async Task<User> RegisterAsync(RegisterDto registerDto)
        {
            if (await _userRepository.ExistsByEmailAsync(registerDto.Email))
            {
                throw new Exception("Email already registered.");
            }

            if (!Enum.TryParse<UserRole>(registerDto.Role, true, out var userRole))
            {
                userRole = UserRole.CUSTOMER;
            }
            
            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = registerDto.FullName,
                Email = registerDto.Email,
                UserName = registerDto.Email,
                PhoneNumber = registerDto.Phone,
                Role = userRole,
                Provider = AuthProvider.LOCAL,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
            {
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return user;
        }

        // [METHOD: LOGIN]
        // Validates user credentials and returns a set of JWT tokens (Access + Refresh).
        // It also checks if the account is currently active.
        public async Task<TokenDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, loginDto.Password))
            {
                throw new UnauthorizedAccessException("Invalid credentials.");
            }

            if (!user.IsActive)
            {
                throw new Exception("Account is deactivated/suspended.");
            }

            return await GenerateTokensAsync(user);
        }

        // [METHOD: REFRESH TOKEN]
        // Allows users to get a new Access Token without logging in again, 
        // as long as their Refresh Token is still valid.
        public async Task<TokenDto> RefreshTokenAsync(string accessToken, string refreshToken)
        {
            try
            {
                var principal = GetPrincipalFromExpiredToken(accessToken);
                var email = principal.FindFirstValue(ClaimTypes.Email);
                var user = await _userManager.FindByEmailAsync(email!);

                if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                {
                    throw new UnauthorizedAccessException("Invalid refresh token.");
                }

                return await GenerateTokensAsync(user);
            }
            catch (Exception ex) when (ex is SecurityTokenException || ex is ArgumentException)
            {
                throw new UnauthorizedAccessException("The access token is malformed or invalid.");
            }
        }

        // [METHOD: PROFILE MANAGEMENT]
        // Retrieves and updates user profile information like phone number and profile picture.
        public async Task<ProfileDto> GetProfileAsync(Guid userId)
        {
            var user = await _userRepository.FindByUserIdAsync(userId);
            if (user == null) throw new Exception("User not found.");

            return MapToProfileDto(user);
        }

        public async Task UpdateProfileAsync(Guid userId, UpdateProfileDto updateDto)
        {
            var user = await _userRepository.FindByUserIdAsync(userId);
            if (user == null) throw new Exception("User not found.");

            user.FullName = updateDto.FullName;
            user.PhoneNumber = updateDto.Phone;
            user.ProfilePicUrl = updateDto.ProfilePicUrl;

            await _userRepository.UpdateAsync(user);
        }

        // [METHOD: SECURITY]
        // Handles password changes and account deactivation for security purposes.
        public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto passwordDto)
        {
            var user = await _userRepository.FindByUserIdAsync(userId);
            if (user == null) throw new Exception("User not found.");

            var result = await _userManager.ChangePasswordAsync(user, passwordDto.OldPassword, passwordDto.NewPassword);
            if (!result.Succeeded)
            {
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        public async Task DeactivateAccountAsync(Guid userId)
        {
            var user = await _userRepository.FindByUserIdAsync(userId);
            if (user == null) throw new Exception("User not found.");

            user.IsActive = false;
            await _userRepository.UpdateAsync(user);
        }

        // [ADMIN METHODS]
        // Administrative tools to manage user accounts, including suspension and deletion.
        public async Task<IEnumerable<ProfileDto>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllUsersAsync();
            return users.Select(MapToProfileDto);
        }

        public async Task SuspendUserAsync(Guid userId, bool suspend)
        {
            var user = await _userRepository.FindByUserIdAsync(userId);
            if (user == null) throw new Exception("User not found.");

            user.IsActive = !suspend;
            await _userRepository.UpdateAsync(user);
        }

        public async Task DeleteUserAsync(Guid userId)
        {
            var user = await _userRepository.FindByUserIdAsync(userId);
            if (user == null) throw new Exception("User not found.");

            await _userManager.DeleteAsync(user);
        }

        // [PRIVATE HELPER: TOKEN GENERATION]
        // Creates the actual JWT string with claims (User ID, Role, etc.) 
        // and signs it with the secret key from appsettings.
        private async Task<TokenDto> GenerateTokensAsync(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddHours(24);

            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                claims,
                expires: expires,
                signingCredentials: creds
            );

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userRepository.UpdateAsync(user);

            return new TokenDto(accessToken, refreshToken, expires);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }

        private ProfileDto MapToProfileDto(User user)
        {
            return new ProfileDto(
                user.Id,
                user.FullName,
                user.Email!,
                user.PhoneNumber!,
                user.ProfilePicUrl,
                user.Role,
                user.IsActive
            );
        }
    }
}
