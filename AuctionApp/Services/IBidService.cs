using System.Collections.Generic;
using System.Threading.Tasks;
using AuctionApp.Models;

namespace AuctionApp.Services
{
    public interface IBidService
    {
        Task<bool> Add(Bid bid);
        Task<IEnumerable<Bid>> List(string auctionId);
    }
}