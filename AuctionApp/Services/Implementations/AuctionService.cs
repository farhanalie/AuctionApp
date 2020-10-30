using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuctionApp.Models;
using AuctionApp.Shared;
using Microsoft.Extensions.Logging;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace AuctionApp.Services.Implementations
{
    public class AuctionService : IAuctionService
    {
        private readonly ILogger<AuctionService> _logger;
        private readonly IRedisDatabase _database;

        public AuctionService(ILogger<AuctionService> logger, IRedisDatabase database)
        {
            _logger = logger;
            _database = database;
        }

        public async Task<IEnumerable<Auction>> List()
        {
            var auctions = await _database.SetMembersAsync<Auction>(Constants.Key.Auctions);
            return auctions.OrderBy(x=>x.ExpiredAt);
        }
    }
}
