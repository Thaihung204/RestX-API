using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace RestX.WebApp.Hubs
{
    public class OrdersHub : Hub
    {
        public const string OrdersCreatedEventName = "orders.created";
    }
}