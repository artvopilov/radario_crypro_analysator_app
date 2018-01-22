using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace CryptoAnalysatorWebApp
{
    public class Program {
        public static void Main(string[] args) {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) {
            int port;
            if (File.Exists("production.on")) {
                port = 80;
            } else {
                port = 5000;
            }
            return WebHost.CreateDefaultBuilder(args)
                .UseSetting(WebHostDefaults.DetailedErrorsKey, "true")
                .CaptureStartupErrors(true)                
                .UseStartup<Startup>()
                .UseKestrel(options => {
                    options.Listen(IPAddress.Any, port);
                })
                .Build();
        }
            
            
    }
}
