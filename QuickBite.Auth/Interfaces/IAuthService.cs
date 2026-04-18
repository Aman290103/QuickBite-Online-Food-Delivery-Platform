using QuickBite.Auth.DTOs;
using QuickBite.Auth.Entities;

namespace QuickBite.Auth.Interfaces
{
    /// <summary>
    /// Authentication and User Management Service
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Registers a new user in the system.
        /// </summary>
        Task<User> RegisterAsync(RegisterDto registerDto);

        /// <summary>
        /// Authenticates a user and returns JWT tokens.
        /// </summary>
        Task<TokenDto> LoginAsync(LoginDto loginDto);

        /// <summary>
        /// Refreshes the access token using a refresh token.
        /// </summary>
        Task<TokenDto> RefreshTokenAsync(string accessToken, string refreshToken);

        /// <summary>
        /// Retrieves the profile details of a user.
        /// </summary>
        Task<ProfileDto> GetProfileAsync(Guid userId);

        /// <summary>
        /// Updates the user's profile information.
        /// </summary>
        Task UpdateProfileAsync(Guid userId, UpdateProfileDto updateDto);

        /// <summary>
        /// Changes the user's password.
        /// </summary>
        Task ChangePasswordAsync(Guid userId, ChangePasswordDto passwordDto);

        /// <summary>
        /// Deactivates the user's account.
        /// </summary>
        Task DeactivateAccountAsync(Guid userId);

        /// <summary>
        /// Retrieves all users (Admin only).
        /// </summary>
        Task<IEnumerable<ProfileDto>> GetAllUsersAsync();

        /// <summary>
        /// Suspends or reactivates a user account (Admin only).
        /// </summary>
        Task SuspendUserAsync(Guid userId, bool suspend);

        /// <summary>
        /// Permanently deletes a user account (Admin only).
        /// </summary>
        Task DeleteUserAsync(Guid userId);
    }
}
