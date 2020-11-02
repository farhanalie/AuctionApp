using System;
using System.Collections.Generic;
using System.Linq;
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
    public class AuctionService : IAuctionService
    {
        private readonly IRedisDatabase _database;
        private readonly IHubContext<BidHub> _hubContext;
        private readonly IBidService _bidService;
        private readonly ILogger<AuctionService> _logger;

        public AuctionService(IRedisDatabase database, IHubContext<BidHub> hubContext, IBidService bidService, ILogger<AuctionService> logger)
        {
            _database = database;
            _hubContext = hubContext;
            _bidService = bidService;
            _logger = logger;
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
            if (auction == null)
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

        public async Task<int> SetMaxBid(UserAuctionMaxBid maxBid)
        {
            var auction = await _database.GetAsync<Auction>(Constants.Key.AuctionBase + maxBid.AuctionId);
            if (auction == null)
                throw new BadRequestException("Invalid auction id provided");
            if (auction.Closed)
                throw new BadRequestException("auction is already closed");

            var userExist = await _database.SetContainsAsync(Constants.Key.Users, new User { UserId = maxBid.UserId });
            if (!userExist)
                throw new BadRequestException("Invalid User id provided");

            var highestBid = await _database.GetAsync<Bid>(Constants.Key.CurrentBidBase + maxBid.AuctionId);
            if (highestBid != null && highestBid.Amount >= maxBid.MaxBid)
                throw new BadRequestException("Cannot set this max amount, as current bid is already equal or greater");

            var added = await _database.SetAddAsync(Constants.Key.UserMaxBidsBase + maxBid.UserId, maxBid);
            if (!added)
                throw new BadRequestException("Same Max Bid is already set for you.");

            var existingMaxBidForAuction =
                await _database.GetAsync<UserAuctionMaxBid>(Constants.Key.AuctionMaxBidBase + maxBid.AuctionId);
            if (existingMaxBidForAuction == null)
            {
                #region first max bid for auction

                added = await _database.AddAsync(Constants.Key.AuctionMaxBidBase + maxBid.AuctionId, maxBid);
                if (added)
                {
                    if (auction.ReservePrice.HasValue)
                    {
                        // Todo: Discuss with client (Q: If there is no bid is placed on auction AND no max bid already exists AND it's more than the reserve price. What should the system do in that case? Shouldn't any amount be placed in such a case? I think we should set it the reserve amount.)
                        var bid = new Bid
                        {
                            AuctionId = auction.AuctionId,
                            UserId = maxBid.UserId,
                            Amount = maxBid.MaxBid <= auction.ReservePrice
                                ? maxBid.MaxBid
                                : auction.OpeningBid.GetValueOrDefault(0),
                        };

                        if (bid.Amount != 0)
                        {
                            added = await _bidService.Add(bid, true);
                            if (added)
                                return maxBid.MaxBid;
                        }

                        return maxBid.MaxBid;
                    }

                    // Todo: Discuss with client (Q: And what should happen if there isn't any reserve price for the above conditions. My guess is we wait for someone to bid but what if no one place a bid and even with a max bid auction no one would win bid.)
                    return maxBid.MaxBid;
                }

                #endregion
            }
            else if (existingMaxBidForAuction.UserId == maxBid.UserId)
            {
                added = await _database.AddAsync(Constants.Key.AuctionMaxBidBase + maxBid.AuctionId, maxBid);
                if (added)
                    return maxBid.MaxBid;
            }
            else
            {
                #region someone already placed a max bid

                var bid = new Bid
                {
                    AuctionId = maxBid.AuctionId,
                };
                //2- If another Maximum bid of the same amount already exists (created by someone else - user B), then user B automatic bid will be placed and the current user A maximum bid is expired and cannot be placed because it is now equal to another bid that has been placed.
                if (existingMaxBidForAuction.MaxBid == maxBid.MaxBid)
                {
                    bid.UserId = existingMaxBidForAuction.UserId;
                    bid.Amount = existingMaxBidForAuction.MaxBid;
                }
                // 3- If another Maximum bid of a lower amount exists (user B), then User's maximum Bid is placed and Users A will bid higher.
                else if (existingMaxBidForAuction.MaxBid < maxBid.MaxBid)
                {
                    bid.UserId = existingMaxBidForAuction.UserId;
                    bid.Amount = existingMaxBidForAuction.MaxBid;
                    await _database.AddAsync<UserAuctionMaxBid>(Constants.Key.AuctionMaxBidBase + maxBid.AuctionId, maxBid);
                }
                // 4- If another Maximum bid of a higher amount exists, then the opposit i.e. user A maximum bid is palced and User B will bid higher.
                else // if (existingMaxBidForAuction.MaxBid > maxBid.MaxBid)
                {
                    bid.UserId = maxBid.UserId;
                    bid.Amount = maxBid.MaxBid;
                }

                var bidAdded = await _bidService.Add(bid, true);
                if (bidAdded)
                    return maxBid.MaxBid;

                #endregion
            }

            // Todo: handle if bid couldn't be added to set
            var error = $"max Bid not added to redis: {maxBid.UserId}, {maxBid.AuctionId}, {maxBid.MaxBid}";
            _logger.Log(LogLevel.Information, error);
            throw new Exception(error);
        }

        public async Task<int> GetMaxBid(string auctionId, string userId)
        {
            var userMaxBidsEnumerable = await _database.SetMembersAsync<UserAuctionMaxBid>(Constants.Key.UserMaxBidsBase + userId);
            var userMaxBidForAuction = userMaxBidsEnumerable.Where(x => x.AuctionId == auctionId)
                .OrderByDescending(x => x.MaxBid).FirstOrDefault();
            if (userMaxBidForAuction == null)
                throw new BadRequestException("No max id found for this user");

            return userMaxBidForAuction.MaxBid;

        }

        private async Task<IEnumerable<string>> GetKeys()
        {
            var auctionKeys = await _database.SetMembersAsync<string>(Constants.Key.AuctionKeys);
            return auctionKeys.Select(x => Constants.Key.AuctionBase + x);
        }

        public async Task CloseAuctions()
        {
            try
            {
                var auctions = await List();
                var expired = auctions.Where(x => x.ExpiredAt <= DateTimeOffset.UtcNow).ToList();
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
                        var updated = await _database.AddAsync(Constants.Key.AuctionBase + auction.AuctionId, auction);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Error, "Exception occurred while closing the job", e);
            }
        }
    }
}
