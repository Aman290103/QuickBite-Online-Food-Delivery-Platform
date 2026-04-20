using Microsoft.EntityFrameworkCore;
using QuickBite.Delivery.Data;
using QuickBite.Delivery.Entities;
using QuickBite.Delivery.Interfaces;

namespace QuickBite.Delivery.Repositories
{
    public class DeliveryRepository : IDeliveryRepository
    {
        private readonly DeliveryDbContext _context;

        public DeliveryRepository(DeliveryDbContext context)
        {
            _context = context;
        }

        public async Task<DeliveryAgent?> GetByIdAsync(Guid agentId) => await _context.DeliveryAgents.FindAsync(agentId);

        public async Task<DeliveryAgent?> GetByUserIdAsync(Guid userId) => await _context.DeliveryAgents.FirstOrDefaultAsync(a => a.UserId == userId);

        public async Task<IEnumerable<DeliveryAgent>> GetAllAsync() => await _context.DeliveryAgents.ToListAsync();

        public async Task AddAsync(DeliveryAgent agent) => await _context.DeliveryAgents.AddAsync(agent);

        public async Task UpdateAsync(DeliveryAgent agent)
        {
            _context.DeliveryAgents.Update(agent);
            await Task.CompletedTask;
        }

        public async Task<IEnumerable<DeliveryAgent>> FindNearbyAgentsAsync(double lat, double lon, double radiusKm)
        {
            // Simple approach: Fetch all online & verified agents and filter using Haversine
            var activeAgents = await _context.DeliveryAgents
                .Where(a => a.IsAvailable && a.IsVerified && a.CurrentLatitude != null && a.CurrentLongitude != null)
                .ToListAsync();

            return activeAgents
                .Select(a => new { Agent = a, Distance = CalculateDistance(lat, lon, a.CurrentLatitude!.Value, a.CurrentLongitude!.Value) })
                .Where(x => x.Distance <= radiusKm)
                .OrderBy(x => x.Distance)
                .Select(x => x.Agent);
        }

        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371; // Earth's radius in Km
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double deg) => deg * (Math.PI / 180);
    }
}
