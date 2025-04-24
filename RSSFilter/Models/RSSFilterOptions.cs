namespace RSSFilter.Models;

public class RSSFilterOptions
{
    public string InputSource { get; set; }
    public string[] TagsToRemove { get; set; } = [];
    public bool CleanupTags { get; set; } = false;
    
    // These TagCleanupSettings are kept for backward compatibility
    public TagCleanupOptions[] TagCleanupSettings { get; set; } = 
    [
        new TagCleanupOptions
        {
            TagName = "title",
            CleanupPattern = @"\sS\d{2}E\d{2}.*"
        },
        new TagCleanupOptions
        {
            TagName = "description",
            CleanupPattern = @"\s\d{3,4}p.*"
        }
    ];
    
    // New settings structure
    public TagSplitOptions[] TagSplit { get; set; } = [];
    public TagCleanupOptions[] TagCleanup { get; set; } = [];
}
