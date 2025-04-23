using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RSSFilter.Models;

namespace RSSFilter;

public class RSSMonitorService : BackgroundService
{
    private readonly RSSFeedUpdate _publisher;
    private readonly RSSFilterOptions _options;
    private readonly IFileLogger _logger;
    private readonly int _maxRetries = 3;
    private readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(5);
    private readonly TimeSpan _pollInterval = TimeSpan.FromMinutes(5);

    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public RSSMonitorService(
        RSSFeedUpdate publisher,
        IOptions<RSSFilterOptions> options,
        IFileLoggerFactory loggerFactory)
    {
        _publisher = publisher;
        _options = options.Value;
        _logger = loggerFactory.CreateLogger("rss_monitor");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.Log($"Checking RSS feed at {DateTime.Now}");

                // Update the cached feed
                await UpdateCachedFeed(stoppingToken);

                _logger.Log("Feed processed and cached successfully.");
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }

            // Wait for the next poll
            await Task.Delay(_pollInterval, stoppingToken);
        }
    }

    private void HandleException(Exception ex)
    {
        string errorDetails = ex switch
        {
            HttpRequestException httpEx => $"HTTP error: {httpEx.Message}, Status: {httpEx.StatusCode}",
            TaskCanceledException => "Request timed out",
            XmlException xmlEx => $"XML parsing error: {xmlEx.Message}",
            _ => $"Unexpected error: {ex.Message}"
        };

        _logger.Log($"Feed processing error: {errorDetails}");

        // Log the stack trace for debugging purposes
        _logger.Log($"Stack trace: {ex.StackTrace}");

        // If there's an inner exception, log it as well
        if (ex.InnerException != null)
        {
            _logger.Log($"Inner exception: {ex.InnerException.Message}");
        }
    }

    private async Task UpdateCachedFeed(CancellationToken cancellationToken)
    {
        try
        {
            // Fetch and process the feed with retry logic
            XDocument processedFeed = await FetchAndProcessFeedWithRetry(cancellationToken);

            // Update the publisher's cache only if we got a valid result
            if (processedFeed != null)
            {
                _publisher.UpdateFeed(processedFeed.ToString());
            }
            else
            {
                _logger.Log("Warning: Feed processing resulted in null document");
            }
        }
        catch (Exception ex)
        {
            _logger.Log($"Failed to update cached feed after retries: {ex.Message}");
            throw; // Rethrow to be caught by the main loop
        }
    }

    public virtual async Task<XDocument> FetchAndProcessFeed(CancellationToken cancellationToken)
    {
        // Load the RSS feed from the configured input source
        string rssContent = await _httpClient.GetStringAsync(_options.InputSource, cancellationToken);

        if (string.IsNullOrEmpty(rssContent))
        {
            throw new InvalidOperationException("Received empty RSS content");
        }

        try
        {
            XDocument xmlDoc = XDocument.Parse(rssContent);

            // Validate the XML structure
            if (xmlDoc.Root == null)
            {
                throw new InvalidOperationException("Invalid RSS feed structure: missing root element");
            }

            // Get the default namespace (if any)
            XNamespace defaultNamespace = xmlDoc.Root.GetDefaultNamespace();

            // Process the feed (remove tags and clean content)
            ProcessFeed(xmlDoc, defaultNamespace);

            return xmlDoc;
        }
        catch (XmlException ex)
        {
            _logger.Log($"XML parsing error: {ex.Message}");
            _logger.Log($"RSS content snippet: {rssContent.Substring(0, Math.Min(100, rssContent.Length))}...");
            throw;
        }
    }

    private async Task<XDocument> FetchAndProcessFeedWithRetry(CancellationToken cancellationToken)
    {
        int attempts = 0;
        Exception lastException = null;

        while (attempts < _maxRetries)
        {
            try
            {
                attempts++;
                _logger.Log($"Attempt {attempts} to fetch RSS feed from {_options.InputSource}");

                return await FetchAndProcessFeed(cancellationToken);
            }
            catch (HttpRequestException ex) when ((int?)ex.StatusCode >= 500 || ex.StatusCode == HttpStatusCode.RequestTimeout)
            {
                // Only retry on server errors or timeouts
                lastException = ex;
                _logger.Log($"Attempt {attempts} failed with server error: {ex.Message}. Retrying in {_retryDelay.TotalSeconds} seconds...");

                if (attempts < _maxRetries)
                {
                    await Task.Delay(_retryDelay, cancellationToken);
                }
            }
            catch (TaskCanceledException)
            {
                lastException = new TimeoutException($"Request timed out on attempt {attempts}");
                _logger.Log($"Request timed out. Retrying in {_retryDelay.TotalSeconds} seconds...");

                if (attempts < _maxRetries)
                {
                    await Task.Delay(_retryDelay, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                // Don't retry on other exceptions
                _logger.Log($"Non-retriable error on attempt {attempts}: {ex.GetType().Name}: {ex.Message}");
                throw;
            }
        }

        _logger.Log($"Failed to fetch RSS feed after {_maxRetries} attempts");
        throw lastException ?? new Exception($"Failed to fetch RSS feed after {_maxRetries} attempts");
    }

    private void ProcessFeed(XDocument xmlDoc, XNamespace defaultNamespace)
    {
        try
        {
            // Get the channel element
            XElement channelElement = xmlDoc.Descendants("channel").FirstOrDefault();

            // Remove specified tags based on configuration
            foreach (string tag in _options.TagsToRemove)
            {
                // Special handling for the link tag - preserve the one directly under channel
                if (tag == "link")
                {
                    // Find all link elements that are NOT direct children of channel
                    IEnumerable<XElement> linkElementsToRemove = xmlDoc.Descendants(defaultNamespace + "link")
                        .Where(element =>
                            element.Parent == null ||
                            element.Parent.Name.LocalName != "channel");

                    // Remove those link elements
                    linkElementsToRemove.Remove();

                    // Log what we've done
                    _logger.Log($"Preserved channel link element, removed {linkElementsToRemove.Count()} other link elements");
                    continue;
                }

                // Standard handling for other tags with namespaces
                if (tag.Contains(':'))
                {
                    // Handle namespaced tags
                    string[] parts = tag.Split(':');
                    XNamespace ns = xmlDoc.Root?.GetNamespaceOfPrefix(parts[0]);

                    if (ns != null)
                    {
                        xmlDoc.Descendants(ns + parts[1]).Remove();
                    }
                    else
                    {
                        _logger.Log($"Warning: Namespace prefix '{parts[0]}' not found in the document");
                    }
                }
                else
                {
                    // Handle non-namespaced tags (except for link, which was handled separately)
                    xmlDoc.Descendants(defaultNamespace + tag).Remove();
                }
            }

            // Only clean up tag content if enabled in configuration
            if (_options.CleanupTags)
            {
                CleanupTagContent(xmlDoc);
            }
        }
        catch (Exception ex)
        {
            _logger.Log($"Error processing feed content: {ex.Message}");
            throw;
        }
    }

    private void CleanupTagContent(XDocument xmlDoc)
    {
        // Process each tag cleanup setting
        foreach (var tagSetting in _options.TagCleanupSettings)
        {
            // Find all elements with the specified tag name
            foreach (XElement element in xmlDoc.Descendants()
                .Where(e => e.Name.LocalName == tagSetting.TagName && !string.IsNullOrEmpty(e.Value)))
            {
                // Apply the regex pattern to clean up the element's value
                element.Value = Regex.Replace(element.Value, tagSetting.CleanupPattern, string.Empty);
            }
        }

        _logger.Log($"Cleaned up content for {_options.TagCleanupSettings.Length} tag types");
    }
}
