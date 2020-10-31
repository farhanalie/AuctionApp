using System;
using System.Collections.Generic;
using System.Linq;
using AuctionApp.Models;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace AuctionApp.Shared
{
    public class DataSeeder
    {
        private readonly IRedisDatabase _database;

        public DataSeeder(IRedisDatabase database)
        {
            _database = database;
        }
        public void SeedData()
        {
            if (!_database.ExistsAsync(Constants.Key.Users).GetAwaiter().GetResult())
            {
                var users = new List<User>
                {
                    new User{UserId = "User1"},
                    new User{UserId = "User2"},
                    new User{UserId = "User3"},
                    new User{UserId = "User4"},
                    new User{UserId = "User5"},
 
                };
                _database.SetAddAllAsync(Constants.Key.Users, CommandFlags.None, users.ToArray()).GetAwaiter().GetResult();
            }

            if (!_database.ExistsAsync(Constants.Key.AuctionKeys).GetAwaiter().GetResult())
            {
                var auctions = new List<Auction>
                {
                    new Auction{AuctionId = "Auction1", ReservePrice = 20000, ExpiredAt = DateTime.UtcNow.AddDays(5), BuyNowPrice = 50000, BuyNowThresholdPrice = 35000},
                    new Auction{AuctionId = "Auction2", ReservePrice = 10000, ExpiredAt = DateTime.UtcNow.AddDays(6)},
                    new Auction{AuctionId = "Auction3", ReservePrice = 5000, ExpiredAt = DateTime.UtcNow.AddMinutes(1), BuyNowPrice = 50000, BuyNowThresholdPrice = 35000},
                    new Auction{AuctionId = "Auction4", ReservePrice = null, ExpiredAt = DateTime.UtcNow.AddDays(8), BuyNowPrice = 50000, BuyNowThresholdPrice = 35000},
                    new Auction{AuctionId = "Auction5", ReservePrice = 20000, ExpiredAt = DateTime.UtcNow.AddDays(9), BuyNowPrice = 50000, BuyNowThresholdPrice = 35000},
                };
                var items = new List<Tuple<string, Auction>>();
                items.AddRange(auctions.Select(auction => new Tuple<string, Auction>(Constants.Key.AuctionBase + auction.AuctionId, auction)));
                var added = _database.AddAllAsync(items).GetAwaiter().GetResult();
                var a = _database.SetAddAllAsync(Constants.Key.AuctionKeys, CommandFlags.None, auctions.Select(x => x.AuctionId).ToArray()).GetAwaiter().GetResult();
            }

        }
    }
}
