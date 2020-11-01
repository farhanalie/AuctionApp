using System.Threading.Tasks;
using AuctionApp.Models;
using AuctionApp.Services;
using AuctionApp.Shared.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace AuctionApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuctionsController : ControllerBase
    {
        private readonly IAuctionService _auctionService;

        public AuctionsController(IAuctionService auctionService)
        {
            _auctionService = auctionService;
        }


        [HttpPost("BuyNow")]
        public async Task<bool> Post([FromBody] Auction auction)
        {
            if (auction?.AuctionId == null)
                throw new BadRequestException("Invalid auction id provided");

            var response = await _auctionService.BuyNow(auction);
            return response;
        }
        
        [HttpPost("SetMaxBid")]
        public async Task<int> SetMaxBid([FromBody] UserAuctionMaxBid request)
        {
            if (request?.AuctionId == null || request.UserId == null || request.MaxBid < 1)
                throw new BadRequestException("Invalid arguments provided");

            var response = await _auctionService.SetMaxBid(request);
            return response;
        }
        
        [HttpGet("GetMaxBid/{auctionId}/{userId}")]
        public async Task<int> GetMaxBid(string auctionId, string userId)
        {
            if (string.IsNullOrEmpty(auctionId) || string.IsNullOrEmpty(userId))
                throw new BadRequestException("Invalid arguments provided");

            var response = await _auctionService.GetMaxBid(auctionId, userId);
            return response;
        }
    }
}
