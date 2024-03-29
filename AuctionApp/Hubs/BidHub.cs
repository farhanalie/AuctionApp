﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace AuctionApp.Hubs
{
    public class BidHub : Hub
    {
        public async Task SubscribeToAuction(string auctionId)
        {
            //throw new Exception("mock failed");
            await Groups.AddToGroupAsync(Context.ConnectionId, auctionId);
        }
        
    }
}
