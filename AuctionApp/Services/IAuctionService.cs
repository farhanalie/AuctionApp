using System.Collections.Generic;
using System.Threading.Tasks;
using AuctionApp.Models;

namespace AuctionApp.Services
{
    public interface IAuctionService
    {
        Task<IEnumerable<Auction>> List();
    }
}