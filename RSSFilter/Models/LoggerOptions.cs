namespace RSSFilter.Models;

public class LoggerOptions
{
    public string LogDirectory { get; set; } = "logs";
    public long MaxFileSizeBytes { get; set; } = 100 * 1024; // 100 KB default
    public int BufferSize { get; set; } = 50;
}
