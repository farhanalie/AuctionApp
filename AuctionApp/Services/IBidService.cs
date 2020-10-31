using System.Collections.Generic;
using System.Threading.Tasks;
using AuctionApp.Models;

namespace AuctionApp.Services
{
    public interface IBidService
    {
        Task<bool> Add(Bid bid, bool bypassHighestRule = false);
        Task<IEnumerable<Bid>> List(string auctionId);
    }
}