using System;

namespace SignalWithRedisDemoApp.Models
{
    public class Bid
    {
        public int BidId { get; set; }
        public string UserId { get; set; }
        public int Amount { get; set; }
        public DateTime CreatedAt { get; set; }

    }
}
