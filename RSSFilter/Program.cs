using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using RSSFilter.Models;

namespace RSSFilter;

internal class Program
{
    static async Task Main(string[] args)
    {
        // get config
        IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        // Create and run the host
        using IHost host = Host.CreateDefaultBuilder(args)

        // Inside Program.cs in the ConfigureServices method
        .ConfigureServices(services =>
        {
            // Register configuration options
            services.Configure<RSSFilterOptions>(config.GetSection("RSSFilter"));
            services.Configure<LoggerOptions>(config.GetSection("Logger"));

            // Register logger factory
            services.AddSingleton<IFileLoggerFactory, FileLoggerFactory>();

            // Register the RSS publisher as a singleton
            services.AddSingleton<RSSFeedUpdate>();

            // Register the RSS monitor service both as concrete type and as hosted service
            services.AddSingleton<RSSMonitorService>();
            services.AddHostedService(sp => sp.GetRequiredService<RSSMonitorService>());
        })
        .ConfigureWebHostDefaults(webBuilder =>
        {
            string configPort = config["Port"];
            int listeningPort = 5000;

            if (!string.IsNullOrWhiteSpace(configPort) && int.TryParse(configPort, out int parsedPort))
            {
                listeningPort = parsedPort;
            }

            webBuilder.UseUrls($"http://0.0.0.0:{listeningPort}");

            webBuilder.Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/rss", async context =>
                    {
                        RSSFeedUpdate publisher = context.RequestServices.GetRequiredService<RSSFeedUpdate>();
                        string feed = publisher.GetLatestFeed();
                        context.Response.ContentType = "application/rss+xml";

                        // Return the cached feed
                        await context.Response.WriteAsync(feed);
                    });

                    // Add a refresh endpoint that forces an immediate feed update
                    endpoints.MapGet("/refresh", async context =>
                    {
                        RSSMonitorService service = context.RequestServices.GetRequiredService<RSSMonitorService>();
                        RSSFeedUpdate publisher = context.RequestServices.GetRequiredService<RSSFeedUpdate>();

                        try
                        {
                            XDocument feed = await service.FetchAndProcessFeed(context.RequestAborted);
                            publisher.UpdateFeed(feed.ToString());

                            await context.Response.WriteAsync("Feed refreshed successfully.");
                        }
                        catch (System.Exception ex)
                        {
                            context.Response.StatusCode = 500;

                            await context.Response.WriteAsync($"Error refreshing feed: {ex.Message}");
                        }
                    });
                });
            });
        })
        .Build();

        await host.RunAsync();
    }
}
