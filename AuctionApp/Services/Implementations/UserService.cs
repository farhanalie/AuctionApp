using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SignalWithRedisDemoApp.Models;
using SignalWithRedisDemoApp.Shared;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace SignalWithRedisDemoApp.Services.Implementations
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