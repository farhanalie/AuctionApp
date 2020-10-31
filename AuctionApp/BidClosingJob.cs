using System;
using System.Threading.Tasks;
using AuctionApp.Services;

namespace AuctionApp
{
    public static class BidClosingJob
    {
        private static IAuctionService _auctionService;

        public static void Start(IAuctionService auctionService)
        {
            _auctionService = auctionService;
            var timer = new System.Threading.Timer(async e => await Callback(),
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1));
        }

        private static async Task Callback()
        {
           await _auctionService.CloseAuctions();
        }
    }
}
