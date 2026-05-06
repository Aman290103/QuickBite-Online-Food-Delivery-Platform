using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using QuickBite.Auth.Entities;

namespace QuickBite.Auth.DTOs
{

    public class RegisterDto
    {
        [JsonPropertyName("fullName")]
        [Required]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [JsonPropertyName("phone")]
        [Required]
        public string Phone { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        [Required]
        public string Role { get; set; } = "CUSTOMER";
    }

    public record LoginDto(
        [Required][EmailAddress] string Email,
        [Required] string Password
    );

    public record ProfileDto(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("fullName")] string FullName,
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("phone")] string Phone,
        [property: JsonPropertyName("profilePicUrl")] string? ProfilePicUrl,
        [property: JsonPropertyName("role")] UserRole Role,
        [property: JsonPropertyName("isActive")] bool IsActive
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
