using System.Collections.Generic;
using System.Threading.Tasks;
using SignalWithRedisDemoApp.Models;

namespace SignalWithRedisDemoApp.Services
{
    public interface IBidService
    {
        Task<bool> Add(Bid bid);
        Task<IEnumerable<Bid>> List(string auctionId);
    }
}