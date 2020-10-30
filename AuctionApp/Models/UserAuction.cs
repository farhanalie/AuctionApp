namespace SignalWithRedisDemoApp.Models
{
    public class UserAuction 
    {
        public string UserId { get; set; }
        public string AuctionId { get; set; }
        public int MaxBid { get; set; }
    }
}