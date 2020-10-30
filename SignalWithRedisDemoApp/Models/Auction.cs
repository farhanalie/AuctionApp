using System;

namespace SignalWithRedisDemoApp.Models
{
    public class Auction
    {
        public string AuctionId { get; set; }
        public int? ReservePrice { get; set; }
        public DateTime? ExpiredAt { get; set; }
    }
}