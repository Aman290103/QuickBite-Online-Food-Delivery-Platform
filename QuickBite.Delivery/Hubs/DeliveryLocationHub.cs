using Microsoft.AspNetCore.SignalR;

namespace QuickBite.Delivery.Hubs
{
    public class DeliveryLocationHub : Hub
    {
        // Hub to join a specific order's tracking group
        public async Task JoinOrderGroup(string orderId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"order-{orderId}");
        }

        public async Task LeaveOrderGroup(string orderId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order-{orderId}");
        }
    }
}
