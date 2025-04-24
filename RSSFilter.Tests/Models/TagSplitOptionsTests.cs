using System.Collections.Generic;
using RSSFilter.Models;

namespace RSSFilter.Tests.Models;

public class TagSplitOptionsTests
{
    [Fact]
    public void TagSplitOptions_Default_Properties_ShouldBeInitialized()
    {
        // Arrange & Act
        TagSplitOptions options = new TagSplitOptions();
        
        // Assert
        Assert.Equal(string.Empty, options.TagName);
        Assert.Equal(string.Empty, options.SplitPattern);
        Assert.NotNull(options.NewTags);
        Assert.Empty(options.NewTags);
    }
    
    [Fact]
    public void TagSplitOptions_CustomValues_ShouldBeSet()
    {
        // Arrange & Act
        TagSplitOptions options = new TagSplitOptions
        {
            TagName = "title",
            SplitPattern = @"(.+S\d{2}(?:E\d{2})?)\\s(.+)",
            NewTags = new Dictionary<string, string>
            {
                { "title", "$1" },
                { "description", "$2" }
            }
        };
        
        // Assert
        Assert.Equal("title", options.TagName);
        Assert.Equal(@"(.+S\d{2}(?:E\d{2})?)\\s(.+)", options.SplitPattern);
        Assert.Equal(2, options.NewTags.Count);
        Assert.Equal("$1", options.NewTags["title"]);
        Assert.Equal("$2", options.NewTags["description"]);
    }
}