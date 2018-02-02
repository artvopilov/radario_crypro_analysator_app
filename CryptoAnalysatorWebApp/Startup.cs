using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using CryptoAnalysatorWebApp.Models;
using CryptoAnalysatorWebApp.Models.Common;
using CryptoAnalysatorWebApp.Interfaces;
using React.AspNet;
using CryptoAnalysatorWebApp.TelegramBot;


namespace CryptoAnalysatorWebApp
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
            services.AddMvc();

            services.AddScoped<ExmoMarket>();
            services.AddScoped<BittrexMarket>();
            services.AddScoped<PoloniexMarket>();
            services.AddScoped<BinanceMarket>();
            services.AddScoped<LivecoinMarket>();
            services.AddScoped<PairsAnalysator>();
                
            Bot.Get();

            //Thread t1 = new Thread(Bot.StartChannelPosting);
            //t1.Start();

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddReact();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=TelegramBot}");
            });

            app.UseFileServer(enableDirectoryBrowsing: true);

            app.Run(async (context) => {
                await context.Response.WriteAsync("Incorrect route!");
            });
        }
    }
}
