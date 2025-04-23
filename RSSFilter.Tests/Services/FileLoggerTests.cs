using System;
using System.IO;

namespace RSSFilter.Tests.Services;

public class FileLoggerTests : IDisposable
{
    private readonly string _testLogPath;

    public FileLoggerTests()
    {
        // Create a unique temporary log file for each test
        _testLogPath = Path.Combine(Path.GetTempPath(), $"test_log_{Guid.NewGuid()}.log");
    }

    public void Dispose()
    {
        // Clean up test files after each test
        if (File.Exists(_testLogPath))
        {
            File.Delete(_testLogPath);
        }
    }

    [Fact]
    public void Log_ShouldCreateLogFile_IfNotExists()
    {
        // Arrange
        IFileLogger logger = new FileLogger(_testLogPath);
        
        // Act
        logger.Log("Test message");
        
        // Assert
        Assert.True(File.Exists(_testLogPath));
        string content = File.ReadAllText(_testLogPath);
        Assert.Contains("Test message", content);
    }

    [Fact]
    public void Log_ShouldAppendToExistingFile()
    {
        // Arrange
        IFileLogger logger = new FileLogger(_testLogPath);
        logger.Log("First message");
        
        // Act
        logger.Log("Second message");
        
        // Assert
        string content = File.ReadAllText(_testLogPath);
        Assert.Contains("First message", content);
        Assert.Contains("Second message", content);
    }

    [Fact]
    public void Log_ShouldTrimOldestEntries_WhenFileSizeExceedsLimit()
    {
        // Arrange - Set a very small file size limit and buffer
        long maxFileSize = 50; // tiny limit to force trimming
        int bufferSize = 1;
        IFileLogger logger = new FileLogger(_testLogPath, maxFileSize, bufferSize);
        
        // Act - Add enough log entries to exceed the size limit
        for (int i = 0; i < 10; i++)
        {
            logger.Log($"Message {i}");
        }

        // Assert - The file should exist and some early logs should be trimmed
        Assert.True(File.Exists(_testLogPath));
        string content = File.ReadAllText(_testLogPath);
        Assert.DoesNotContain("Message 0", content); // First message should be trimmed
        Assert.Contains("Message 9", content); // Last message should still be there
        Assert.Contains("[LOG TRIMMED:", content); // Trim message should be present
    }
}