using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SignalWithRedisDemoApp.Hubs;
using SignalWithRedisDemoApp.Middleware;
using SignalWithRedisDemoApp.Services;
using SignalWithRedisDemoApp.Services.Implementations;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.System.Text.Json;

namespace SignalWithRedisDemoApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            //services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("RedisCs"));
            var redisConfiguration = Configuration.GetSection("Redis").Get<RedisConfiguration>();
            services.AddStackExchangeRedisExtensions<SystemTextJsonSerializer>(redisConfiguration);
            services.AddControllers();
            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            });
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAuctionService, AuctionService>();
            services.AddScoped<IBidService, BidService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            // Add Error Handling Middleware
            app.UseMiddleware(typeof(ErrorHandlingMiddleware));
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UserRedisInformation();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapHub<BidHub>("/bidhub");
            });
        }
    }
}
