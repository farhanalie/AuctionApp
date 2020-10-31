using System;
using System.Collections.Generic;
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


        [HttpPost]
        public async Task<bool> Post([FromBody] Auction auction)
        {
            if (auction?.AuctionId == null)
                throw new BadRequestException("Invalid auction id provided");

            var response = await _auctionService.BuyNow(auction);
            return response;
        }
    }
}
