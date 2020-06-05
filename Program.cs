using System;
using System.Threading.Tasks;
using Gateway.Extensions.Configuration;
using Gateway.Extensions.Utils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Ditto
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .Enrich.WithProperty("Version", ReflectionUtils.GetAssemblyVersion<Program>())
                .Enrich.WithMachineName()
                .CreateLogger();

            try
            {
                IHostBuilder hostBuilder = CreateHostBuilder(args);
                await hostBuilder.Build().RunAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Application start-up failed");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog((hostContext, loggerConfiguration) =>
                {
                    loggerConfiguration.ReadFrom.Configuration(hostContext.Configuration);
                })
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config
                        .AddEnvironmentVariables(prefix: "FLOW_")
                        .AddAwsSecrets();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();

                    webBuilder.ConfigureKestrel(options =>
                    {
                        options.AddServerHeader = false;

                        // Currently, the App.Metrics ASCIIFormatter doesn't support async write.
                        // The Prometheus formatter we uses the ASCIIFormatter under the hood.
                        // Should be fixed in App.Metrics 4.0 at that point we can remove this again.
                        options.AllowSynchronousIO = true;
                    });    
                });
    }
}