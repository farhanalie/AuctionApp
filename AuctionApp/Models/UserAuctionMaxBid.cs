namespace AuctionApp.Models
{
    public class UserAuctionMaxBid 
    {
        public string UserId { get; set; }
        public string AuctionId { get; set; }
        public int MaxBid { get; set; }
    }
}