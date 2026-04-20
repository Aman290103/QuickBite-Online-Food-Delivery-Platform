using QuickBite.Delivery.DTOs;

namespace QuickBite.Delivery.Interfaces
{
    public interface IDeliveryService
    {
        Task<AgentResponseDto> RegisterAgentAsync(Guid userId, RegisterAgentDto dto);
        Task<AgentResponseDto> GetProfileAsync(Guid agentId);
        Task<IEnumerable<AgentResponseDto>> GetNearbyAgentsAsync(double lat, double lon, double radiusKm);
        
        Task VerifyAgentAsync(Guid agentId);
        Task SetAvailabilityAsync(Guid agentId, bool isAvailable);
        
        Task UpdateLocationAsync(Guid agentId, UpdateLocationDto dto);
        
        Task AssignOrderAsync(Guid agentId, Guid orderId);
        Task CompleteDeliveryAsync(Guid agentId);
        Task UpdateRatingAsync(Guid agentId, decimal rating);
    }
}
