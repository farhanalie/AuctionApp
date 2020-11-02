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

        public async Task<bool> Add(Bid bid, bool bypassRules = false)
        {
            var currentBid = await _database.GetAsync<Bid>(Constants.Key.CurrentBidBase + bid.AuctionId);
            if (!bypassRules)
            {
                if (currentBid != null && bid.Amount <= currentBid.Amount)
                    throw new BadRequestException("amount should be more than max bid");
            
                if (currentBid != null && bid.UserId == currentBid.UserId)
                    throw new BadRequestException("You are already the highest bidder");
            }
            var auction = await _database.GetAsync<Auction>(Constants.Key.AuctionBase + bid.AuctionId);
            if (auction?.Closed == true)
                throw new BadRequestException("Invalid auction id provided");

            var maxBid = await _database.GetAsync<UserAuctionMaxBid>(Constants.Key.AuctionMaxBidBase + bid.AuctionId);
            if (maxBid== null)
            {
                var added = await AddBidAndNotify(bid, auction);
                if (added)
                    return true;
            }
            else if (maxBid.MaxBid == bid.Amount)
            {
                // consume max
                bid.UserId = maxBid.UserId;
                var added = await AddBidAndNotify(bid, auction);
                if (added)
                    return true;
            }
            else if (maxBid.MaxBid > bid.Amount)
            {
                // if its from current user, he have placed a bid by now so we skip his bid
                if (currentBid!=null && bid.UserId==currentBid.UserId)
                {
                    // consume max
                    bid.UserId = maxBid.UserId;
                    bid.Amount += 1000;
                    var added = await AddBidAndNotify(bid,auction);
                    if (added)
                        return true;
                }
                else
                {
                    var added = await AddBidAndNotify(bid, auction);

                    if (added)
                    {
                        // consume max
                        bid.UserId = maxBid.UserId;
                        bid.Amount += 1000;
                        added = await AddBidAndNotify(bid, auction);
                        if (added)
                            return true;
                    }
                }
            }
            else if (maxBid.MaxBid < bid.Amount)
            {
                
                // if its from current user he have placed a bid by now so we skip it 
                if (currentBid != null && maxBid.MaxBid == currentBid.Amount)
                {
                    var added = await AddBidAndNotify(bid, auction);
                    if (added)
                        return true;
                }
                else
                {
                    // consume max
                    var consumeMaxBid = new Bid
                    {
                        UserId = maxBid.UserId,
                        AuctionId = bid.AuctionId,
                        Amount = maxBid.MaxBid
                    };
                    var added = await AddBidAndNotify(consumeMaxBid, auction);
                    if (added)
                    {
                        added = await AddBidAndNotify(bid, auction);
                        if (added)
                            return true;
                    }
                }
            }

            // Todo: handle if bid couldn't be added to set
            var error = $"Bid not added to redis set: {bid.UserId}, {bid.Amount}";
            _logger.Log(LogLevel.Information, error);
            throw new Exception(error);
        }

        private async Task<bool> AddBidAndNotify(Bid bid, Auction auction)
        {
            bid.CreatedAt = DateTime.UtcNow;
            bid.BidId = Guid.NewGuid();
            var added = await _database.SetAddAsync(Constants.Key.BidsBase + bid.AuctionId, bid);
            await _database.AddAsync(Constants.Key.CurrentBidBase + bid.AuctionId, bid);
            if (added)
            {
                if ((auction.ExpiredAt - bid.CreatedAt).TotalSeconds <= 60)
                {
                    auction.ExpiredAt = auction.ExpiredAt.AddMinutes(1);
                    var auctionAdded = await _database.AddAsync(Constants.Key.AuctionBase + bid.AuctionId, auction);
                }
#pragma warning disable 4014
                _hubContext.Clients.Group(bid.AuctionId).SendAsync("ReceiveBid", bid, auction.ExpiredAt);
            #pragma warning restore 4014
            }
            return added;
        }

        public async Task<IEnumerable<Bid>> List(string auctionId)
        {
            var bids = await _database.SetMembersAsync<Bid>(Constants.Key.BidsBase + auctionId);
            return bids;
        }
    }
}
