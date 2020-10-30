using System;

namespace AuctionApp.Models
{
    public class Auction
    {
        public string AuctionId { get; set; }
        public int? ReservePrice { get; set; }
        public DateTimeOffset? ExpiredAt { get; set; }
    }
}