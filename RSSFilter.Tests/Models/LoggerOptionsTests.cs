using RSSFilter.Models;

namespace RSSFilter.Tests.Models;

public class LoggerOptionsTests
{
    [Fact]
    public void LoggerOptions_Default_Properties_ShouldBeCorrect()
    {
        // Arrange & Act
        LoggerOptions options = new LoggerOptions();
        
        // Assert
        Assert.Equal("logs", options.LogDirectory);
        Assert.Equal(100 * 1024, options.MaxFileSizeBytes); // 100 KB default
        Assert.Equal(50, options.BufferSize);
    }
    
    [Fact]
    public void LoggerOptions_CustomValues_ShouldBeSet()
    {
        // Arrange & Act
        LoggerOptions options = new LoggerOptions
        {
            LogDirectory = "custom_logs",
            MaxFileSizeBytes = 500 * 1024,
            BufferSize = 25
        };
        
        // Assert
        Assert.Equal("custom_logs", options.LogDirectory);
        Assert.Equal(500 * 1024, options.MaxFileSizeBytes);
        Assert.Equal(25, options.BufferSize);
    }
}