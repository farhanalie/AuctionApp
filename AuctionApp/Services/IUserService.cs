using System.Collections.Generic;
using System.Threading.Tasks;
using AuctionApp.Models;

namespace AuctionApp.Services
{
    public interface IUserService
    {
        Task<IEnumerable<User>> List();
    }
}
