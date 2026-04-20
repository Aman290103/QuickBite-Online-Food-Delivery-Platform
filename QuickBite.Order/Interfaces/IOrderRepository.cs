using QuickBite.Order.Entities;

namespace QuickBite.Order.Interfaces
{
    public interface IOrderRepository
    {
        Task<Entities.Order?> GetByIdAsync(Guid id);
        Task<IEnumerable<Entities.Order>> GetByCustomerIdAsync(Guid customerId);
        Task<IEnumerable<Entities.Order>> GetByRestaurantIdAsync(Guid restaurantId);
        Task<IEnumerable<Entities.Order>> GetByStatusAsync(OrderStatus status);
        Task<IEnumerable<Entities.Order>> GetByAgentIdAsync(Guid agentId);
        Task<IEnumerable<Entities.Order>> GetAllAsync();
        
        Task AddAsync(Entities.Order order);
        Task UpdateAsync(Entities.Order order);
        Task SaveChangesAsync();
    }
}
