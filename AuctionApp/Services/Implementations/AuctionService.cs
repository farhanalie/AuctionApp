using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuctionApp.Hubs;
using AuctionApp.Models;
using AuctionApp.Shared;
using AuctionApp.Shared.Exceptions;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace AuctionApp.Services.Implementations
{
    public class AuctionService : IAuctionService
    {
        private readonly IRedisDatabase _database;
        private readonly IHubContext<BidHub> _hubContext;
        private readonly IBidService _bidService;

        public AuctionService(IRedisDatabase database, IHubContext<BidHub> hubContext, IBidService bidService)
        {
            _database = database;
            _hubContext = hubContext;
            _bidService = bidService;
        }

        public async Task<IEnumerable<Auction>> List()
        {
            var auctionKeys = await GetKeys();
            var auctions = await _database.GetAllAsync<Auction>(auctionKeys);
            return auctions.Values.Where(x => x.Closed == false).OrderBy(x => x.AuctionId);
        }

        public async Task<bool> BuyNow(Auction request)
        {
            var auction = await _database.GetAsync<Auction>(Constants.Key.AuctionBase + request.AuctionId);
            if (auction==null)
                throw new BadRequestException("Invalid auction id provided");

            if (auction.BuyNowPrice == null)
                throw new BadRequestException("Auction doesn't have a buy now price");

            if (auction.Closed)
                throw new BadRequestException("Auction is already closed");

            var highestBid = await _database.GetAsync<Bid>(Constants.Key.CurrentBidBase + auction.AuctionId);
            if (highestBid != null && highestBid.Amount >= auction.BuyNowPrice)
                throw new BadRequestException("Auction cannot be buy now, as it already have a equal or greater bid");

            var bid = new Bid
            {
                AuctionId = auction.AuctionId,
                UserId = request.WinnerUserId,
                Amount = auction.BuyNowPrice.Value,
            };

            var bidAdded = await _bidService.Add(bid, true);
            if (bidAdded)
            {
                auction.Closed = true;
                auction.WinnerUserId = request.WinnerUserId;
                var added = await _database.AddAsync(Constants.Key.AuctionBase + auction.AuctionId, auction);
                if (added)
                {
#pragma warning disable 4014
                    _hubContext.Clients.Group(auction.AuctionId).SendAsync("AuctionClosed", auction.WinnerUserId);
#pragma warning restore 4014
                }

                return added;
            }
            throw new Exception("failed to buy now");
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
#pragma warning disable 4014
                        _hubContext.Clients.Group(auction.AuctionId).SendAsync("AuctionClosed", null);

                    }
                    else if (auction.ReservePrice.HasValue && highestBid.Amount < auction.ReservePrice)
                    {
                        _hubContext.Clients.Group(auction.AuctionId).SendAsync("AuctionClosed", null);
                    }
                    else
                    {
                        auction.WinnerUserId = highestBid.UserId;
                        _hubContext.Clients.Group(auction.AuctionId).SendAsync("AuctionClosed", auction.WinnerUserId);
                    }
#pragma warning restore 4014
                }
                var items = new List<Tuple<string, Auction>>();
                items.AddRange(expired.Select(auction => new Tuple<string, Auction>(Constants.Key.AuctionBase + auction.AuctionId, auction)));
                var updated = await _database.AddAllAsync(items);
            }
        }
    }
}
