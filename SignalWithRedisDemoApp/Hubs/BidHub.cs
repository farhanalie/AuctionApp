using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SignalWithRedisDemoApp.Models;

namespace SignalWithRedisDemoApp.Hubs
{
    public class BidHub : Hub
    {
        public async Task PlaceBid(Bid bid)
        {
            // Todo: add to redis
            bid.CreatedAt = DateTime.UtcNow;
            await Clients.All.SendAsync("ReceiveBid", bid);
        }
    }
}
