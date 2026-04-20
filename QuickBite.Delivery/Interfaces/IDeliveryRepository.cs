using QuickBite.Delivery.Entities;

namespace QuickBite.Delivery.Interfaces
{
    public interface IDeliveryRepository
    {
        Task<DeliveryAgent?> GetByIdAsync(Guid agentId);
        Task<DeliveryAgent?> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<DeliveryAgent>> GetAllAsync();
        
        Task AddAsync(DeliveryAgent agent);
        Task UpdateAsync(DeliveryAgent agent);
        
        Task<IEnumerable<DeliveryAgent>> FindNearbyAgentsAsync(double lat, double lon, double radiusKm);
        Task SaveChangesAsync();
    }
}
