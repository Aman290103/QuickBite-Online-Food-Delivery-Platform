using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using QuickBite.Delivery.DTOs;
using QuickBite.Delivery.Entities;
using QuickBite.Delivery.Hubs;
using QuickBite.Delivery.Interfaces;
using System.Text.Json;

namespace QuickBite.Delivery.Services
{
    // [SERVICE: DELIVERY TRACKING]
    // This service manages the lifecycle of a delivery agent (rider).
    // It handles onboarding, availability, and real-time GPS location broadcasting.
    public class DeliveryService : IDeliveryService
    {
        private readonly IDeliveryRepository _repository;
        private readonly IHubContext<DeliveryLocationHub> _hubContext;
        private readonly IDistributedCache _cache;
        private readonly ILogger<DeliveryService> _logger;

        public DeliveryService(
            IDeliveryRepository repository, 
            IHubContext<DeliveryLocationHub> hubContext, 
            IDistributedCache cache,
            ILogger<DeliveryService> logger)
        {
            _repository = repository;
            _hubContext = hubContext;
            _cache = cache;
            _logger = logger;
        }

        // [METHOD: AGENT ONBOARDING]
        public async Task<AgentResponseDto> RegisterAgentAsync(Guid userId, RegisterAgentDto dto)
        {
            var existing = await _repository.GetByUserIdAsync(userId);
            if (existing != null) throw new Exception("User is already registered as an agent.");

            var agent = new DeliveryAgent
            {
                AgentId = Guid.NewGuid(),
                UserId = userId,
                FullName = dto.FullName,
                Phone = dto.Phone,
                VehicleType = dto.VehicleType,
                VehicleNumber = dto.VehicleNumber,
                IsAvailable = false,
                IsVerified = false
            };

            await _repository.AddAsync(agent);
            await _repository.SaveChangesAsync();

            return MapToDto(agent);
        }

        // [METHOD: PROFILE & DISCOVERY]
        public async Task<AgentResponseDto> GetProfileAsync(Guid agentId)
        {
            var agent = await _repository.GetByIdAsync(agentId);
            if (agent == null) throw new Exception("Agent not found.");
            return MapToDto(agent);
        }

        public async Task<IEnumerable<AgentResponseDto>> GetNearbyAgentsAsync(double lat, double lon, double radiusKm)
        {
            var agents = await _repository.FindNearbyAgentsAsync(lat, lon, radiusKm);
            return agents.Select(MapToDto);
        }

        // [METHOD: ADMIN & STATUS]
        public async Task VerifyAgentAsync(Guid agentId)
        {
            var agent = await _repository.GetByIdAsync(agentId);
            if (agent == null) throw new Exception("Agent not found.");

            agent.IsVerified = true;
            await _repository.UpdateAsync(agent);
            await _repository.SaveChangesAsync();
        }

        public async Task SetAvailabilityAsync(Guid agentId, bool isAvailable)
        {
            var agent = await _repository.GetByIdAsync(agentId);
            if (agent == null) throw new Exception("Agent not found.");

            agent.IsAvailable = isAvailable;
            await _repository.UpdateAsync(agent);
            await _repository.SaveChangesAsync();
        }

        // [METHOD: LIVE LOCATION TRACKING]
        public async Task UpdateLocationAsync(Guid agentId, UpdateLocationDto dto)
        {
            var agent = await _repository.GetByIdAsync(agentId);
            if (agent == null) throw new Exception("Agent not found.");

            agent.CurrentLatitude = dto.Latitude;
            agent.CurrentLongitude = dto.Longitude;

            await _repository.UpdateAsync(agent);
            await _repository.SaveChangesAsync();

            var cacheKey = $"agent_location:{agentId}";
            var locationData = JsonSerializer.Serialize(new { agentId, dto.Latitude, dto.Longitude });
            await _cache.SetStringAsync(cacheKey, locationData, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
            });

            if (dto.OrderId.HasValue)
            {
                await _hubContext.Clients.Group($"order-{dto.OrderId}")
                    .SendAsync("ReceiveLocation", agentId, dto.Latitude, dto.Longitude);
            }
        }

        // [METHOD: ASSIGNMENT & COMPLETION]
        public async Task AssignOrderAsync(Guid agentId, Guid orderId)
        {
            var agent = await _repository.GetByIdAsync(agentId);
            if (agent == null) throw new Exception("Agent not found.");
            if (!agent.IsAvailable || !agent.IsVerified) throw new Exception("Agent is not available.");

            agent.IsAvailable = false;
            await _repository.UpdateAsync(agent);
            await _repository.SaveChangesAsync();
        }

        public async Task CompleteDeliveryAsync(Guid agentId)
        {
            var agent = await _repository.GetByIdAsync(agentId);
            if (agent == null) throw new Exception("Agent not found.");

            agent.TotalDeliveries++;
            agent.IsAvailable = true;
            await _repository.UpdateAsync(agent);
            await _repository.SaveChangesAsync();
        }

        public async Task UpdateRatingAsync(Guid agentId, decimal rating)
        {
            var agent = await _repository.GetByIdAsync(agentId);
            if (agent == null) throw new Exception("Agent not found.");

            agent.AvgRating = (agent.AvgRating * agent.TotalDeliveries + rating) / (agent.TotalDeliveries + 1);
            await _repository.UpdateAsync(agent);
            await _repository.SaveChangesAsync();
        }

        private AgentResponseDto MapToDto(DeliveryAgent a) => new AgentResponseDto(
            a.AgentId, a.UserId, a.FullName, a.Phone, a.VehicleType, a.VehicleNumber,
            a.IsAvailable, a.IsVerified, a.AvgRating, a.TotalDeliveries,
            a.CurrentLatitude, a.CurrentLongitude
        );
    }
}
