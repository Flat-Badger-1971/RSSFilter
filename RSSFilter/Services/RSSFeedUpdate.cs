using System;

namespace RSSFilter;

public class RSSFeedUpdate
{
    private string _latestFeed = string.Empty;
    private readonly IFileLogger _logger;
    private DateTime _lastUpdated = DateTime.MinValue;

    public RSSFeedUpdate(IFileLoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger("rss_update");
    }

    public virtual void UpdateFeed(string feed)
    {
        _latestFeed = feed;
        _lastUpdated = DateTime.Now;
        _logger.Log($"Feed updated at {_lastUpdated}");
    }

    public virtual string GetLatestFeed()
    {
        _logger.Log($"Feed requested. Last updated: {_lastUpdated}");

        return _latestFeed;
    }
}
