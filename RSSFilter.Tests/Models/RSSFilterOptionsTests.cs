using RSSFilter.Models;
using System.Collections.Generic;
using System.Linq;

namespace RSSFilter.Tests.Models;

public class RSSFilterOptionsTests
{
    [Fact]
    public void RSSFilterOptions_Default_Properties()
    {
        // Arrange & Act
        RSSFilterOptions options = new RSSFilterOptions();
        
        // Assert
        Assert.False(options.CleanupTags);
        Assert.NotNull(options.TagCleanupSettings);
        Assert.Equal(2, options.TagCleanupSettings.Length);

        // Check the default tag cleanup patterns
        TagCleanupOptions titleCleanup = options.TagCleanupSettings.FirstOrDefault(t => t.TagName == "title");
        Assert.NotNull(titleCleanup);
        Assert.Equal(@"\sS\d{2}E\d{2}.*", titleCleanup.CleanupPattern);

        TagCleanupOptions descriptionCleanup = options.TagCleanupSettings.FirstOrDefault(t => t.TagName == "description");
        Assert.NotNull(descriptionCleanup);
        Assert.Equal(@"\s\d{3,4}p.*", descriptionCleanup.CleanupPattern);
        
        // Check new properties
        Assert.NotNull(options.TagSplit);
        Assert.Empty(options.TagSplit);
        Assert.NotNull(options.TagCleanup);
        Assert.Empty(options.TagCleanup);
    }
    
    [Fact]
    public void RSSFilterOptions_CustomValues_ShouldBeSet()
    {
        // Arrange & Act
        var tagSplit = new TagSplitOptions
        {
            TagName = "title",
            SplitPattern = @"(.+S\d{2}(?:E\d{2})?)\\s(.+)",
            NewTags = new Dictionary<string, string>
            {
                { "title", "$1" },
                { "description", "$2" }
            }
        };
        
        var tagCleanup = new TagCleanupOptions
        {
            TagName = "description",
            CleanupPattern = @"\s(?:\d{3,4}p)|(?:RERIP).*"
        };
        
        RSSFilterOptions options = new RSSFilterOptions
        {
            InputSource = "http://test.com/feed.xml",
            TagsToRemove = ["guid", "pubDate"],
            CleanupTags = true,
            TagSplit = [tagSplit],
            TagCleanup = [tagCleanup]
        };
        
        // Assert
        Assert.Equal("http://test.com/feed.xml", options.InputSource);
        Assert.Equal(2, options.TagsToRemove.Length);
        Assert.Contains("guid", options.TagsToRemove);
        Assert.Contains("pubDate", options.TagsToRemove);
        Assert.True(options.CleanupTags);
        
        // Check TagSplit settings
        Assert.Single(options.TagSplit);
        Assert.Equal("title", options.TagSplit[0].TagName);
        Assert.Equal(@"(.+S\d{2}(?:E\d{2})?)\\s(.+)", options.TagSplit[0].SplitPattern);
        Assert.Equal(2, options.TagSplit[0].NewTags.Count);
        
        // Check TagCleanup settings
        Assert.Single(options.TagCleanup);
        Assert.Equal("description", options.TagCleanup[0].TagName);
        Assert.Equal(@"\s(?:\d{3,4}p)|(?:RERIP).*", options.TagCleanup[0].CleanupPattern);
    }
}