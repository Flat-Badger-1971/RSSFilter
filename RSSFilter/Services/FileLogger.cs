using System;
using System.IO;

namespace RSSFilter;

public class FileLogger : IFileLogger
{
    private readonly string _logFilePath;
    private readonly long _maxFileSizeBytes;
    private readonly object _lockObject = new();
    private readonly int _bufferSize;

    public FileLogger(string logFilePath, long maxFileSizeBytes = 100 * 1024, int bufferSize = 50)
    {
        _logFilePath = logFilePath;
        _maxFileSizeBytes = maxFileSizeBytes;
        _bufferSize = bufferSize;

        // Ensure log directory exists
        string logDirectory = Path.GetDirectoryName(_logFilePath);

        if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }
    }

    public void Log(string message)
    {
        try
        {
            lock (_lockObject)
            {
                // Create the log file if it doesn't exist
                if (!File.Exists(_logFilePath))
                {
                    using (File.Create(_logFilePath)) { }
                }

                // Check if trimming is needed
                FileInfo fileInfo = new(_logFilePath);

                if (fileInfo.Length > _maxFileSizeBytes)
                {
                    TrimOldestEntries();
                }

                // Append the log message to the file
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}";
                File.AppendAllText(_logFilePath, logEntry);
            }
        }
        catch (Exception ex)
        {
            // If logging fails, write to the console as a fallback
            Console.WriteLine($"Failed to write to log file: {ex.Message}");
        }
    }

    private void TrimOldestEntries()
    {
        try
        {
            // Read all lines from the file
            string[] allLines = File.ReadAllLines(_logFilePath);

            // If the file is small enough, just return
            if (allLines.Length <= _bufferSize)
            {
                return;
            }

            // Keep only the newest entries (skip the oldest ones)
            string[] newLines = new string[allLines.Length - _bufferSize];
            Array.Copy(allLines, _bufferSize, newLines, 0, newLines.Length);

            // Write the trimmed content back to the file
            File.WriteAllLines(_logFilePath, newLines);

            // Add a note that entries were trimmed
            string trimMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - [LOG TRIMMED: Removed {_bufferSize} oldest entries to maintain size limit]{Environment.NewLine}";
            File.AppendAllText(_logFilePath, trimMessage);
        }
        catch (Exception ex)
        {
            // If trimming fails, log to console
            Console.WriteLine($"Failed to trim log file: {ex.Message}");
        }
    }
}
