using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuctionApp.Models;
using AuctionApp.Shared;
using Microsoft.Extensions.Logging;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace AuctionApp.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly ILogger<UserService> _logger;
        private readonly IRedisDatabase _database;

        public UserService(ILogger<UserService> logger, IRedisDatabase database)
        {
            _logger = logger;
            _database = database;
        }

        public async Task<IEnumerable<User>> List()
        {
            var auctions = await _database.SetMembersAsync<User>(Constants.Key.Users);
            return auctions.OrderBy(x=>x.UserId);
        }
    }
}