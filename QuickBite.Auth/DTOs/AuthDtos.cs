using System.ComponentModel.DataAnnotations;
using QuickBite.Auth.Entities;

namespace QuickBite.Auth.DTOs
{
    public record RegisterDto(
        [Required] string FullName,
        [Required][EmailAddress] string Email,
        [Required][MinLength(6)] string Password,
        [Required] string Phone,
        [Required] UserRole Role
    );

    public record LoginDto(
        [Required][EmailAddress] string Email,
        [Required] string Password
    );

    public record ProfileDto(
        string FullName,
        string Email,
        string Phone,
        string? ProfilePicUrl,
        UserRole Role,
        bool IsActive
    );

    public record TokenDto(
        string AccessToken,
        string RefreshToken,
        DateTime ExpiresAt
    );

    public record ChangePasswordDto(
        [Required] string OldPassword,
        [Required][MinLength(6)] string NewPassword
    );

    public record UpdateProfileDto(
        string FullName,
        string Phone,
        string? ProfilePicUrl
    );
}
