using QuickBite.Auth.Entities;

namespace QuickBite.Auth.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> FindByEmailAsync(string email);
        Task<User?> FindByUserIdAsync(Guid userId);
        Task<IEnumerable<User>> FindAllByRoleAsync(UserRole role);
        Task<bool> ExistsByEmailAsync(string email);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task UpdateAsync(User user);
    }
}
