using System;
using System.Collections.Generic;
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

            if (!_database.ExistsAsync(Constants.Key.Auctions).GetAwaiter().GetResult())
            {
                var auctions = new List<Auction>
                {
                    new Auction{AuctionId = "Auction1", ReservePrice = null, ExpiredAt = DateTime.UtcNow.AddDays(5)},
                    new Auction{AuctionId = "Auction2", ReservePrice = 100000, ExpiredAt = DateTime.UtcNow.AddDays(6)},
                    new Auction{AuctionId = "Auction3", ReservePrice = 50000, ExpiredAt = DateTime.UtcNow.AddDays(7)},
                    new Auction{AuctionId = "Auction4", ReservePrice = null, ExpiredAt = DateTime.UtcNow.AddDays(8)},
                    new Auction{AuctionId = "Auction5", ReservePrice = 200000, ExpiredAt = DateTime.UtcNow.AddDays(9)},
                };
                _database.SetAddAllAsync(Constants.Key.Auctions, CommandFlags.None, auctions.ToArray()).GetAwaiter().GetResult(); ;

            }

        }
    }
}
