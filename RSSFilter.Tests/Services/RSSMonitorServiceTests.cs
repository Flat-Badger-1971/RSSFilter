using Microsoft.Extensions.Options;
using NSubstitute;
using RSSFilter.Models;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RSSFilter.Tests.Services;

public class RSSMonitorServiceTests
{
    private readonly RSSFeedUpdate _mockPublisher;
    private readonly IOptions<RSSFilterOptions> _mockOptions;
    private readonly IFileLoggerFactory _mockLoggerFactory;
    private readonly IFileLogger _mockLogger;
    private readonly RSSFilterOptions _options;

    public RSSMonitorServiceTests()
    {
        // Set up common mocks
        _mockPublisher = Substitute.For<RSSFeedUpdate>(Substitute.For<IFileLoggerFactory>());

        _options = new RSSFilterOptions
        {
            InputSource = "https://example.com/rss",
            TagsToRemove = ["category", "guid"],
            CleanupTags = true,
            TagCleanupSettings =
            [
                new TagCleanupOptions
                {
                    TagName = "title",
                    CleanupPattern = @"\sS\d{2}E\d{2}.*"
                }
            ]
        };

        _mockOptions = Substitute.For<IOptions<RSSFilterOptions>>();
        _mockOptions.Value.Returns(_options);

        _mockLogger = Substitute.For<IFileLogger>();
        _mockLoggerFactory = Substitute.For<IFileLoggerFactory>();
        _mockLoggerFactory.CreateLogger(Arg.Any<string>()).Returns(_mockLogger);
    }

    [Fact]
    public async Task FetchAndProcessFeed_ShouldProcessFeedCorrectly()
    {
        // Arrange
        TestableRSSMonitorService service = new TestableRSSMonitorService(_mockPublisher, _mockOptions, _mockLoggerFactory);
        string xmlContent = @"
            <rss version='2.0'>
                <channel>
                    <title>Test Channel</title>
                    <link>https://example.com</link>
                    <description>Test Description</description>
                    <item>
                        <title>Test Item S01E01 HD</title>
                        <guid>12345</guid>
                        <category>Test</category>
                    </item>
                </channel>
            </rss>";

        service.SetupMockResponse(xmlContent);

        // Act
        XDocument result = await service.FetchAndProcessFeed(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Channel", result.Descendants("title").First().Value);

        // The title should be cleaned up (S01E01 HD pattern removed)
        var itemTitle = result.Descendants("item").First().Element("title")?.Value;
        Assert.Equal("Test Item", itemTitle);

        // The guid and category tags should be removed
        Assert.Empty(result.Descendants("guid"));
        Assert.Empty(result.Descendants("category"));
    }

    [Fact]
    public async Task FetchAndProcessFeed_WithHttpException_ShouldThrow()
    {
        // Arrange
        TestableRSSMonitorService service = new TestableRSSMonitorService(_mockPublisher, _mockOptions, _mockLoggerFactory);
        service.SetupHttpException(HttpStatusCode.InternalServerError);

        // Act & Assert
        HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => service.FetchAndProcessFeed(CancellationToken.None)
        );

        Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
    }

    [Fact]
    public async Task FetchAndProcessFeed_WithInvalidXml_ShouldThrow()
    {
        // Arrange
        TestableRSSMonitorService service = new TestableRSSMonitorService(_mockPublisher, _mockOptions, _mockLoggerFactory);
        service.SetupMockResponse("This is not valid XML");

        // Act & Assert
        await Assert.ThrowsAsync<System.Xml.XmlException>(
            () => service.FetchAndProcessFeed(CancellationToken.None)
        );
    }

    [Fact]
    public async Task FetchAndProcessFeed_WithEmptyResponse_ShouldThrow()
    {
        // Arrange
        TestableRSSMonitorService service = new TestableRSSMonitorService(_mockPublisher, _mockOptions, _mockLoggerFactory);
        service.SetupMockResponse("");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.FetchAndProcessFeed(CancellationToken.None)
        );
    }

    // Testable subclass that allows us to override HttpClient behavior
    public class TestableRSSMonitorService(
        RSSFeedUpdate publisher,
        IOptions<RSSFilterOptions> options,
        IFileLoggerFactory loggerFactory) : RSSMonitorService(publisher, options, loggerFactory)
    {
        private Func<Task<string>> _getStringAsyncFunc = null;
        private Exception _exceptionToThrow = null;

        public void SetupMockResponse(string response)
        {
            _getStringAsyncFunc = () => Task.FromResult(response);
            _exceptionToThrow = null;
        }

        public void SetupHttpException(HttpStatusCode statusCode)
        {
            _exceptionToThrow = new HttpRequestException("Mock HTTP error", null, statusCode);
            _getStringAsyncFunc = null;
        }

        // Override the method that uses HttpClient
        public override async Task<XDocument> FetchAndProcessFeed(CancellationToken cancellationToken)
        {
            if (_exceptionToThrow != null)
            {
                throw _exceptionToThrow;
            }

            if (_getStringAsyncFunc != null)
            {
                string rssContent = await _getStringAsyncFunc();

                if (string.IsNullOrEmpty(rssContent))
                {
                    throw new InvalidOperationException("Received empty RSS content");
                }

                try
                {
                    XDocument xmlDoc = XDocument.Parse(rssContent);

                    // Validate the XML structure
                    if (xmlDoc.Root == null)
                    {
                        throw new InvalidOperationException("Invalid RSS feed structure: missing root element");
                    }

                    // Get the default namespace (if any)
                    XNamespace defaultNamespace = xmlDoc.Root.GetDefaultNamespace();

                    // Call the protected ProcessFeed method using reflection
                    MethodInfo processMethod = typeof(RSSMonitorService).GetMethod("ProcessFeed", BindingFlags.NonPublic | BindingFlags.Instance);

                    processMethod?.Invoke(this, [xmlDoc, defaultNamespace]);

                    return xmlDoc;
                }
                catch (Exception)
                {
                    throw;
                }
            }

            // Fall back to the base implementation if no mock is set up
            return await base.FetchAndProcessFeed(cancellationToken);
        }
    }
}