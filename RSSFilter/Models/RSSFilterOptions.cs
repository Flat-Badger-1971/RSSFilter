namespace RSSFilter.Models;

public class RSSFilterOptions
{
    public string InputSource { get; set; }
    public string[] TagsToRemove { get; set; }
    public bool CleanupTags { get; set; } = false;
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
}
