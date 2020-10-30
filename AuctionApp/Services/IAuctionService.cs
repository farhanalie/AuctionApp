using System.Collections.Generic;
using System.Threading.Tasks;
using SignalWithRedisDemoApp.Models;

namespace SignalWithRedisDemoApp.Services
{
    public interface IAuctionService
    {
        Task<IEnumerable<Auction>> List();
    }
}