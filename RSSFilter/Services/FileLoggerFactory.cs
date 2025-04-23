using System.IO;
using Microsoft.Extensions.Options;
using RSSFilter.Models;

namespace RSSFilter;

public class FileLoggerFactory : IFileLoggerFactory
{
    private readonly LoggerOptions _options;

    public FileLoggerFactory(IOptions<LoggerOptions> options)
    {
        _options = options.Value;
    }

    public IFileLogger CreateLogger(string logName)
    {
        string logPath = Path.Combine(_options.LogDirectory, $"{logName}.log");

        return new FileLogger(logPath, _options.MaxFileSizeBytes, _options.BufferSize);
    }
}
