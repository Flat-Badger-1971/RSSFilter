using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RSSFilter.Models;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RSSFilter.Tests.Integration;

public class WebEndpointIntegrationTests : IDisposable
{
    private readonly TestServer _server;
    private readonly HttpClient _client;
    private readonly RSSFeedUpdate _feedUpdate;
    private readonly string _testLogDir;

    public WebEndpointIntegrationTests()
    {
        _testLogDir = Path.Combine(Path.GetTempPath(), $"rss_test_logs_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testLogDir);

        // Build a test server with our app setup
        IHostBuilder hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/rss", async context =>
                        {
                            RSSFeedUpdate publisher = context.RequestServices.GetRequiredService<RSSFeedUpdate>();
                            string feed = publisher.GetLatestFeed();
                            context.Response.ContentType = "application/rss+xml";

                            await context.Response.WriteAsync(feed);
                        });

                        endpoints.MapGet("/refresh", async context =>
                        {
                            RSSMonitorService service = context.RequestServices.GetRequiredService<RSSMonitorService>();
                            RSSFeedUpdate publisher = context.RequestServices.GetRequiredService<RSSFeedUpdate>();

                            try
                            {
                                // For testing, we'll manually trigger the first step
                                MethodInfo feedMethod = typeof(RSSMonitorService).GetMethod("UpdateCachedFeed", BindingFlags.NonPublic | BindingFlags.Instance);

                                Task task = feedMethod.Invoke(service, [CancellationToken.None]) as Task;
                                await task;

                                await context.Response.WriteAsync("Feed refreshed successfully.");
                            }
                            catch (Exception ex)
                            {
                                context.Response.StatusCode = 500;
                                await context.Response.WriteAsync($"Error refreshing feed: {ex.Message}");
                            }
                        });
                    });
                });

                // Configure the test services
                webHost.ConfigureServices(services =>
                {
                    // Add routing services required for endpoints
                    services.AddRouting();
                    
                    // Mock options
                    IOptions<LoggerOptions> loggerOptions = Options.Create(new LoggerOptions
                    {
                        LogDirectory = _testLogDir,
                        MaxFileSizeBytes = 5000,
                        BufferSize = 10
                    });

                    IOptions<RSSFilterOptions> rssOptions = Options.Create(new RSSFilterOptions
                    {
                        InputSource = "https://example.com/test.xml",
                        TagsToRemove = ["guid"],
                        CleanupTags = true
                    });

                    // Set up real services
                    services.AddSingleton(loggerOptions);
                    services.AddSingleton(rssOptions);
                    services.AddSingleton<IFileLoggerFactory, FileLoggerFactory>();
                    services.AddSingleton<RSSFeedUpdate>();

                    // Add a testable monitor service
                    services.AddSingleton<RSSMonitorService>(sp => new TestableMonitorService(
                        sp.GetRequiredService<RSSFeedUpdate>(),
                        rssOptions,
                        sp.GetRequiredService<IFileLoggerFactory>()
                    ));
                });
            });

        // Start the test server
        IHost host = hostBuilder.Start();
        _server = host.GetTestServer();
        _client = _server.CreateClient();

        // Keep a reference to the RSSFeedUpdate service to set up test data
        _feedUpdate = _server.Services.GetRequiredService<RSSFeedUpdate>();

        // Pre-populate with a test feed
        _feedUpdate.UpdateFeed("<rss><channel><title>Test Feed</title></channel></rss>");

        // Get the monitor service and set up a test feed
        TestableMonitorService monitor = _server.Services.GetRequiredService<RSSMonitorService>() as TestableMonitorService;
        monitor.TestFeed = @"
            <rss version='2.0'>
                <channel>
                    <title>Refreshed Feed</title>
                    <item>
                        <title>Test Item</title>
                        <guid>12345</guid>
                    </item>
                </channel>
            </rss>";
    }

    public void Dispose()
    {
        _client.Dispose();
        _server.Dispose();

        // Clean up test log directory
        try
        {
            if (Directory.Exists(_testLogDir))
            {
                Directory.Delete(_testLogDir, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public async Task RSSEndpoint_ShouldReturnCachedFeed()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync("/rss");
        string content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/rss+xml", response.Content.Headers.ContentType.MediaType);
        Assert.Contains("<title>Test Feed</title>", content);
    }

    [Fact]
    public async Task RefreshEndpoint_ShouldUpdateFeed()
    {
        // Act - First get the initial feed
        HttpResponseMessage initialResponse = await _client.GetAsync("/rss");
        string initialContent = await initialResponse.Content.ReadAsStringAsync();

        // Then trigger a refresh
        HttpResponseMessage refreshResponse = await _client.GetAsync("/refresh");
        string refreshContent = await refreshResponse.Content.ReadAsStringAsync();

        // Then get the updated feed
        HttpResponseMessage finalResponse = await _client.GetAsync("/rss");
        string finalContent = await finalResponse.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        Assert.Contains("Feed refreshed successfully", refreshContent);
        Assert.Contains("<title>Refreshed Feed</title>", finalContent);
        Assert.DoesNotContain("<guid>", finalContent); // Tags should be removed
    }

    [Fact]
    public async Task RefreshEndpoint_WithError_ShouldReturn500()
    {
        // Arrange - Set up an error in the test monitor
        TestableMonitorService monitor = _server.Services.GetRequiredService<RSSMonitorService>() as TestableMonitorService;
        monitor.ShouldThrowException = true;

        // Act
        HttpResponseMessage response = await _client.GetAsync("/refresh");
        string content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("Error refreshing feed", content);
    }

    // Test subclass for the RSSMonitorService
    private class TestableMonitorService(
        RSSFeedUpdate publisher,
        IOptions<RSSFilterOptions> options,
        IFileLoggerFactory loggerFactory) : RSSMonitorService(publisher, options, loggerFactory)
    {
        public string TestFeed { get; set; }
        public bool ShouldThrowException { get; set; }

        public override async Task<XDocument> FetchAndProcessFeed(CancellationToken cancellationToken)
        {
            if (ShouldThrowException)
            {
                throw new Exception("Test exception");
            }

            XDocument doc = XDocument.Parse(TestFeed);

            // Call the protected ProcessFeed method using reflection
            MethodInfo processMethod = typeof(RSSMonitorService).GetMethod("ProcessFeed", BindingFlags.NonPublic | BindingFlags.Instance);

            processMethod?.Invoke(this,
            [
                doc,
                doc.Root?.GetDefaultNamespace() ?? XNamespace.None
            ]);

            return doc;
        }
    }
}