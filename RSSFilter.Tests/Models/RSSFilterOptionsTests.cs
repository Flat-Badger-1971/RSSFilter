using RSSFilter.Models;
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
    }
    
    [Fact]
    public void RSSFilterOptions_CustomValues_ShouldBeSet()
    {
        // Arrange & Act
        RSSFilterOptions options = new RSSFilterOptions
        {
            InputSource = "http://test.com/feed.xml",
            TagsToRemove = ["guid", "pubDate"],
            CleanupTags = true,
            TagCleanupSettings =
            [
                new TagCleanupOptions
                {
                    TagName = "custom",
                    CleanupPattern = "pattern"
                }
            ]
        };
        
        // Assert
        Assert.Equal("http://test.com/feed.xml", options.InputSource);
        Assert.Equal(2, options.TagsToRemove.Length);
        Assert.Contains("guid", options.TagsToRemove);
        Assert.Contains("pubDate", options.TagsToRemove);
        Assert.True(options.CleanupTags);
        Assert.Single(options.TagCleanupSettings);
        Assert.Equal("custom", options.TagCleanupSettings[0].TagName);
        Assert.Equal("pattern", options.TagCleanupSettings[0].CleanupPattern);
    }
}