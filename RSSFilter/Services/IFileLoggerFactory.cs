namespace RSSFilter;

public interface IFileLoggerFactory
{
    IFileLogger CreateLogger(string logName);
}