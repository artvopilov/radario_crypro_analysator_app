using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            services.AddSingleton<PairsAnalysator>();
            services.AddSingleton<TimeService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.Run(async (context) => {
                await context.Response.WriteAsync("Incorrect route!");
            });
        }
    }
}
