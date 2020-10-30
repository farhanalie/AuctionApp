using System.Collections.Generic;
using System.Threading.Tasks;
using AuctionApp.Models;
using AuctionApp.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace AuctionApp.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IUserService _userService;
        private readonly IAuctionService _auctionService;

        public IndexModel(ILogger<IndexModel> logger, IUserService userService, IAuctionService auctionService)
        {
            _logger = logger;
            _userService = userService;
            _auctionService = auctionService;
        }

        public async Task OnGetAsync()
        {
            Users = await _userService.List();
            Auctions = await _auctionService.List();
        }

        public IEnumerable<Auction> Auctions { get; set; }

        public IEnumerable<User> Users { get; set; }
    }
}
