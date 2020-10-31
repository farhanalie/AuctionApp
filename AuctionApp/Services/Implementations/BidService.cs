using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AuctionApp.Hubs;
using AuctionApp.Models;
using AuctionApp.Shared;
using AuctionApp.Shared.Exceptions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace AuctionApp.Services.Implementations
{
    public class BidService : IBidService
    {
        private readonly ILogger<BidService> _logger;
        private readonly IRedisDatabase _database;
        private readonly IHubContext<BidHub> _hubContext;

        public BidService(ILogger<BidService> logger, IRedisDatabase database, IHubContext<BidHub> hubContext)
        {
            _logger = logger;
            _database = database;
            _hubContext = hubContext;
        }

        public async Task<bool> Add(Bid bid, bool bypassHighestRule = false)
        {
            var currentBid = await _database.GetAsync<Bid>(Constants.Key.CurrentBidBase + bid.AuctionId);
            if (currentBid != null && bid.Amount <= currentBid.Amount)
                throw new BadRequestException("amount should be more than max bid");
            
            if (bypassHighestRule == false && currentBid != null && bid.UserId == currentBid.UserId)
                throw new BadRequestException("You are already the highest bidder");

            bid.CreatedAt = DateTime.UtcNow;
            bid.BidId = Guid.NewGuid();
            var added = await _database.SetAddAsync(Constants.Key.BidsBase+bid.AuctionId, bid);
            await _database.AddAsync(Constants.Key.CurrentBidBase+bid.AuctionId, bid);
            if (added)
            {
#pragma warning disable 4014
                _hubContext.Clients.Group(bid.AuctionId).SendAsync("ReceiveBid", bid);
#pragma warning restore 4014
                return added;
            }

            // Todo: handle if bid couldn't be added to set
            var error = $"Bid not added to redis set: {bid.UserId}, {bid.Amount}";
            _logger.Log(LogLevel.Information, error);
            throw new Exception(error);
        }

        public async Task<IEnumerable<Bid>> List(string auctionId)
        {
            var bids = await _database.SetMembersAsync<Bid>(Constants.Key.BidsBase + auctionId);
            return bids;
        }
    }
}
