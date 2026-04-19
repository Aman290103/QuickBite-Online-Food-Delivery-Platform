using Microsoft.EntityFrameworkCore;
using QuickBite.Auth.Data;
using QuickBite.Auth.Entities;
using QuickBite.Auth.Interfaces;

namespace QuickBite.Auth.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AuthDbContext _context;

        public UserRepository(AuthDbContext context)
        {
            _context = context;
        }

        public async Task<User?> FindByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> FindByUserIdAsync(Guid userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task<IEnumerable<User>> FindAllByRoleAsync(UserRole role)
        {
            return await _context.Users.Where(u => u.Role == role).ToListAsync();
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}
