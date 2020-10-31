using System;

namespace AuctionApp.Models
{
    public class Auction
    {
        public string AuctionId { get; set; }
        public int? ReservePrice { get; set; }
        public DateTimeOffset? ExpiredAt { get; set; }
        public bool Closed { get; set; }
        public string WinnerUserId { get; set; }
        public int? BuyNowPrice { get; set; }
        public int? BuyNowThresholdPrice { get; set; }
    }
}