using Microsoft.Extensions.Options;
using NSubstitute;
using RSSFilter.Models;
using System;
using System.IO;
using System.Reflection;

namespace RSSFilter.Tests.Services;

public class FileLoggerFactoryTests
{
    [Fact]
    public void CreateLogger_ShouldReturnFileLogger_WithCorrectPath()
    {
        // Arrange
        LoggerOptions options = new LoggerOptions
        {
            LogDirectory = "test_logs",
            MaxFileSizeBytes = 5000,
            BufferSize = 10
        };

        IOptions<LoggerOptions> mockOptions = Substitute.For<IOptions<LoggerOptions>>();
        mockOptions.Value.Returns(options);

        FileLoggerFactory factory = new FileLoggerFactory(mockOptions);

        // Act
        IFileLogger logger = factory.CreateLogger("test");

        // Assert
        Assert.IsType<FileLogger>(logger);

        // We can't directly access private fields, but we can use reflection to verify them
        Type loggerType = logger.GetType();
        FieldInfo fieldInfo = loggerType.GetField("_logFilePath", BindingFlags.NonPublic | BindingFlags.Instance);
        string actualPath = fieldInfo?.GetValue(logger) as string;

        string expectedPath = Path.Combine("test_logs", "test.log");
        Assert.Equal(expectedPath, actualPath);
    }

    [Fact]
    public void CreateLogger_DifferentLogNames_ShouldCreateDifferentFiles()
    {
        // Arrange
        LoggerOptions options = new LoggerOptions
        {
            LogDirectory = "test_logs"
        };

        IOptions<LoggerOptions> mockOptions = Substitute.For<IOptions<LoggerOptions>>();
        mockOptions.Value.Returns(options);

        FileLoggerFactory factory = new FileLoggerFactory(mockOptions);

        // Act
        IFileLogger logger1 = factory.CreateLogger("service1");
        IFileLogger logger2 = factory.CreateLogger("service2");

        // Assert
        Type loggerType = typeof(FileLogger);

        FieldInfo fieldInfo = loggerType.GetField("_logFilePath", BindingFlags.NonPublic | BindingFlags.Instance);
        string path1 = fieldInfo?.GetValue(logger1) as string;
        string path2 = fieldInfo?.GetValue(logger2) as string;

        Assert.NotEqual(path1, path2);
        Assert.Contains("service1.log", path1);
        Assert.Contains("service2.log", path2);
    }
}