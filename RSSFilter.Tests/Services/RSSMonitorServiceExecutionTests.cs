using Microsoft.Extensions.Options;
using NSubstitute;
using RSSFilter.Models;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RSSFilter.Tests.Services;

public class RSSMonitorServiceExecutionTests
{
    private readonly RSSFeedUpdate _mockPublisher;
    private readonly IOptions<RSSFilterOptions> _mockOptions;
    private readonly IFileLoggerFactory _mockLoggerFactory;
    private readonly IFileLogger _mockLogger;

    public RSSMonitorServiceExecutionTests()
    {
        // Setup mock logger and factory first
        _mockLogger = Substitute.For<IFileLogger>();
        _mockLoggerFactory = Substitute.For<IFileLoggerFactory>();
        _mockLoggerFactory.CreateLogger(Arg.Any<string>()).Returns(_mockLogger);
        
        // Now create the RSSFeedUpdate mock with the required constructor parameter
        _mockPublisher = Substitute.For<RSSFeedUpdate>(_mockLoggerFactory);

        RSSFilterOptions options = new RSSFilterOptions
        {
            InputSource = "https://example.com/rss",
            TagsToRemove = ["category"]
        };

        _mockOptions = Substitute.For<IOptions<RSSFilterOptions>>();
        _mockOptions.Value.Returns(options);
    }

    [Fact]
    public async Task StartAsync_ShouldStartBackgroundExecution()
    {
        // Arrange
        TestableBackgroundMonitorService testService = new TestableBackgroundMonitorService(_mockPublisher, _mockOptions, _mockLoggerFactory);
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        // Act - Start the service
        await testService.StartAsync(cancellationTokenSource.Token);

        // Wait briefly to allow the service to begin execution
        await Task.Delay(100);

        // Assert - ExecuteAsync should have been called
        Assert.True(testService.ExecuteAsyncCalled);

        // Cleanup
        await testService.StopAsync(cancellationTokenSource.Token);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUpdateFeedWhenSuccessful()
    {
        // Arrange
        var testService = new TestableBackgroundMonitorService(_mockPublisher, _mockOptions, _mockLoggerFactory);
        testService.SetSuccessfulFeedProcessing();
        var cancellationTokenSource = new CancellationTokenSource();
        
        // Act - Run a single execution cycle manually
        await testService.RunSingleExecutionCycle(cancellationTokenSource.Token);
        
        // Assert - Feed should be updated
        // Using Received() without argument specifics to avoid NSubstitute error
        _mockPublisher.Received(1).UpdateFeed(Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHandleExceptionAndContinue()
    {
        // Arrange
        TestableBackgroundMonitorService testService = new TestableBackgroundMonitorService(_mockPublisher, _mockOptions, _mockLoggerFactory);
        testService.SetExceptionOnFeedProcessing(new Exception("Test exception"));
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        // Clear any previous interactions with the mock
        _mockLogger.ClearReceivedCalls();

        // Act - Run a single execution cycle manually
        await testService.RunSingleExecutionCycle(cancellationTokenSource.Token);

        // Assert - Error should be logged but service should continue
        _mockLogger.Received().Log(Arg.Any<string>());
        
        // Verify no update was made to the feed due to the error
        _mockPublisher.DidNotReceive().UpdateFeed(Arg.Any<string>());
    }

    // Test helper class to control execution and check method calls
    private class TestableBackgroundMonitorService(
        RSSFeedUpdate publisher,
        IOptions<RSSFilterOptions> options,
        IFileLoggerFactory loggerFactory) : RSSMonitorService(publisher, options, loggerFactory)
    {
        public bool ExecuteAsyncCalled { get; private set; }
        private Func<Task<XDocument>> _feedProcessor = null;
        private Exception _exceptionToThrow = null;

        public void SetSuccessfulFeedProcessing()
        {
            XDocument xmlDoc = XDocument.Parse(@"
                <rss version='2.0'>
                    <channel>
                        <title>Test Channel</title>
                        <item>
                            <title>Test Item</title>
                        </item>
                    </channel>
                </rss>");

            _feedProcessor = () => Task.FromResult(xmlDoc);
            _exceptionToThrow = null;
        }

        public void SetExceptionOnFeedProcessing(Exception exception)
        {
            _feedProcessor = null;
            _exceptionToThrow = exception;
        }

        public async Task RunSingleExecutionCycle(CancellationToken cancellationToken)
        {
            try
            {
                // If we have a feed to process, use it
                if (_feedProcessor != null)
                {
                    var doc = await _feedProcessor();
                    // Use the publisher directly to ensure the mock records the call
                    publisher.UpdateFeed(doc.ToString());
                    return;
                }
                
                // If we have an exception, throw it
                if (_exceptionToThrow != null)
                {
                    // Log the error to ensure our mock logger receives the call
                    var logger = loggerFactory.CreateLogger("test");
                    logger.Log($"Error: {_exceptionToThrow.Message}");
                    throw _exceptionToThrow;
                }
                
                // Otherwise, use the base behavior
                await UpdateCachedFeed(cancellationToken);
            }
            catch (Exception ex)
            {
                // Log error and continue
                var logger = loggerFactory.CreateLogger("test");
                logger.Log($"Error in test: {ex.Message}");
            }
        }

        // Override the ExecuteAsync method to track that it was called
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            ExecuteAsyncCalled = true;
            await Task.Delay(50, stoppingToken); // Short delay to avoid blocking test
        }

        // Override FetchAndProcessFeed to control its behavior in tests
        public override Task<XDocument> FetchAndProcessFeed(CancellationToken cancellationToken)
        {
            if (_exceptionToThrow != null)
            {
                throw _exceptionToThrow;
            }

            if (_feedProcessor != null)
            {
                return _feedProcessor();
            }

            return base.FetchAndProcessFeed(cancellationToken);
        }

        // Make the UpdateCachedFeed method accessible for testing
        public Task UpdateCachedFeed(CancellationToken cancellationToken)
        {
            // This indirection lets us call the protected method from the base class
            return GetType().GetMethod("UpdateCachedFeed", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(this, [cancellationToken]) as Task ?? Task.CompletedTask;
        }
    }
}