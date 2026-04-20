namespace QuickBite.Delivery.Entities
{
    public enum VehicleType
    {
        BIKE,
        SCOOTER,
        CAR,
        BICYCLE
    }

    public class DeliveryAgent
    {
        public Guid AgentId { get; set; }
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        
        public VehicleType VehicleType { get; set; }
        public string VehicleNumber { get; set; } = string.Empty;

        public double? CurrentLatitude { get; set; }
        public double? CurrentLongitude { get; set; }

        public bool IsAvailable { get; set; } = false;
        public bool IsVerified { get; set; } = false;

        public decimal AvgRating { get; set; } = 5.0m;
        public int TotalDeliveries { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
