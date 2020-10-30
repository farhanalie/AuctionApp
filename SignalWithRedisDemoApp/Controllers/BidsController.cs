using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SignalWithRedisDemoApp.Models;
using SignalWithRedisDemoApp.Services;

namespace SignalWithRedisDemoApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BidsController : ControllerBase
    {
        private readonly IBidService _bidService;

        public BidsController(IBidService bidService)
        {
            _bidService = bidService;
        }

        [HttpGet("{auctionId}")]
        public async Task<IEnumerable<Bid>> Get(string auctionId)
        {
           return await _bidService.List(auctionId);
        }

        [HttpPost]
        public async Task<bool> Post([FromBody] Bid bid)
        {
           var r =  await _bidService.Add(bid);
           return r;
        }
    }
}
