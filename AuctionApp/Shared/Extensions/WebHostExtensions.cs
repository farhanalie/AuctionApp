﻿using AuctionApp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace AuctionApp.Shared.Extensions
{
    public static class WebHostExtensions
    {
        public static IHost SeedData(this IHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var database = services.GetService<IRedisDatabase>();
                new DataSeeder(database).SeedData();
                return host;
            }
        }
        public static IHost StartBidClosingService(this IHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var service = services.GetService<IAuctionService>();
                BidClosingJob.Start(service);
                return host;
            }
        }
    }
}
