using RSSFilter.Models;

namespace RSSFilter.Tests.Models;

public class TagCleanupOptionsTests
{
    [Fact]
    public void TagCleanupOptions_Default_Properties_ShouldBeEmptyStrings()
    {
        // Arrange & Act
        TagCleanupOptions options = new TagCleanupOptions();
        
        // Assert
        Assert.Equal(string.Empty, options.TagName);
        Assert.Equal(string.Empty, options.CleanupPattern);
    }
    
    [Fact]
    public void TagCleanupOptions_CustomValues_ShouldBeSet()
    {
        // Arrange & Act
        TagCleanupOptions options = new TagCleanupOptions
        {
            TagName = "title",
            CleanupPattern = @"\d+\s+"
        };
        
        // Assert
        Assert.Equal("title", options.TagName);
        Assert.Equal(@"\d+\s+", options.CleanupPattern);
    }
}