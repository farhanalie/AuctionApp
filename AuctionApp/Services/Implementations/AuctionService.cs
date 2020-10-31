using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuctionApp.Hubs;
using AuctionApp.Models;
using AuctionApp.Shared;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace AuctionApp.Services.Implementations
{
    public class AuctionService : IAuctionService
    {
        private readonly IRedisDatabase _database;
        private readonly IHubContext<BidHub> _hubContext;

        public AuctionService(IRedisDatabase database, IHubContext<BidHub> hubContext)
        {
            _database = database;
            _hubContext = hubContext;
        }

        public async Task<IEnumerable<Auction>> List()
        {
            var auctionKeys = await GetKeys();
            var auctions = await _database.GetAllAsync<Auction>(auctionKeys);
            return auctions.Values.Where(x => x.Closed == false).OrderBy(x => x.AuctionId);
        }

        private async Task<IEnumerable<string>> GetKeys()
        {
            var auctionKeys = await _database.SetMembersAsync<string>(Constants.Key.AuctionKeys);
            return auctionKeys.Select(x => Constants.Key.AuctionBase + x);
        }

        public async Task CloseAuctions()
        {
            var auctions = await List();
            var expired = auctions.Where(x => x.ExpiredAt <= DateTime.UtcNow).ToList();
            if (expired.Any())
            {
                foreach (var auction in expired)
                {
                    auction.Closed = true;

                    var highestBid = await _database.GetAsync<Bid>(Constants.Key.CurrentBidBase + auction.AuctionId);
                    if (highestBid == null)
                    {
                        await _hubContext.Clients.Group(auction.AuctionId).SendAsync("AuctionClosed", null);
                    }
                    else if (auction.ReservePrice.HasValue && highestBid.Amount < auction.ReservePrice)
                    {
                        await _hubContext.Clients.Group(auction.AuctionId).SendAsync("AuctionClosed", null);
                    }
                    else
                    {
                        auction.WinnerUserId = highestBid.UserId;
                        await _hubContext.Clients.Group(auction.AuctionId).SendAsync("AuctionClosed", auction.WinnerUserId);
                    }
                }
                var items = new List<Tuple<string, Auction>>();
                items.AddRange(expired.Select(auction => new Tuple<string, Auction>(Constants.Key.AuctionBase + auction.AuctionId, auction)));
                var updated = await _database.AddAllAsync(items);
            }
        }
    }
}
