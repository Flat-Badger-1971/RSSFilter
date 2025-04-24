namespace RSSFilter.Models;

public interface IFileLoggerFactory
{
    IFileLogger CreateLogger(string name);
}
