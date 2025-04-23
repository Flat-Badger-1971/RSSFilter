using NSubstitute;

namespace RSSFilter.Tests.Services;

public class RSSFeedUpdateTests
{
    private readonly IFileLoggerFactory _mockLoggerFactory;
    private readonly IFileLogger _mockLogger;

    public RSSFeedUpdateTests()
    {
        // Setup mocks
        _mockLogger = Substitute.For<IFileLogger>();
        _mockLoggerFactory = Substitute.For<IFileLoggerFactory>();
        _mockLoggerFactory.CreateLogger(Arg.Any<string>()).Returns(_mockLogger);
    }

    [Fact]
    public void UpdateFeed_ShouldUpdateLatestFeed()
    {
        // Arrange
        RSSFeedUpdate service = new RSSFeedUpdate(_mockLoggerFactory);
        string testFeed = "<rss><channel><title>Test Feed</title></channel></rss>";

        // Act
        service.UpdateFeed(testFeed);
        string result = service.GetLatestFeed();

        // Assert
        Assert.Equal(testFeed, result);
    }

    [Fact]
    public void GetLatestFeed_WhenNothingUpdated_ShouldReturnEmptyString()
    {
        // Arrange
        RSSFeedUpdate service = new RSSFeedUpdate(_mockLoggerFactory);

        // Act
        string result = service.GetLatestFeed();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GetLatestFeed_AfterUpdate_ShouldReturnLatestFeed()
    {
        // Arrange
        RSSFeedUpdate service = new RSSFeedUpdate(_mockLoggerFactory);
        string testFeed1 = "<rss><channel><title>First Feed</title></channel></rss>";
        string testFeed2 = "<rss><channel><title>Updated Feed</title></channel></rss>";

        // Act
        service.UpdateFeed(testFeed1);
        service.UpdateFeed(testFeed2);
        string result = service.GetLatestFeed();

        // Assert
        Assert.Equal(testFeed2, result);
        Assert.NotEqual(testFeed1, result);
    }
}