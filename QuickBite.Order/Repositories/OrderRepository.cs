using Microsoft.EntityFrameworkCore;
using QuickBite.Order.Data;
using QuickBite.Order.Entities;
using QuickBite.Order.Interfaces;

namespace QuickBite.Order.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly OrderDbContext _context;

        public OrderRepository(OrderDbContext context)
        {
            _context = context;
        }

        public async Task<Entities.Order?> GetByIdAsync(Guid id)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id);
        }

        public async Task<IEnumerable<Entities.Order>> GetByCustomerIdAsync(Guid customerId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Entities.Order>> GetByRestaurantIdAsync(Guid restaurantId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.RestaurantId == restaurantId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Entities.Order>> GetByStatusAsync(OrderStatus status)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.Status == status)
                .ToListAsync();
        }

        public async Task<IEnumerable<Entities.Order>> GetByAgentIdAsync(Guid agentId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.DeliveryAgentId == agentId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Entities.Order>> GetAllAsync()
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task AddAsync(Entities.Order order)
        {
            await _context.Orders.AddAsync(order);
        }

        public async Task UpdateAsync(Entities.Order order)
        {
            _context.Orders.Update(order);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
