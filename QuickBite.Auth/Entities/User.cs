using Microsoft.AspNetCore.Identity;

namespace QuickBite.Auth.Entities
{
    public enum UserRole
    {
        CUSTOMER = 0,
        OWNER = 1,
        DELIVERY_AGENT = 2,
        ADMIN = 3
    }

    public enum AuthProvider
    {
        LOCAL,
        GOOGLE,
        GITHUB
    }

    public class User : IdentityUser<Guid>
    {
        public string FullName { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.CUSTOMER;
        public AuthProvider Provider { get; set; } = AuthProvider.LOCAL;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? ProfilePicUrl { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
    }
}
