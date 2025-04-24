using System.Collections.Generic;

namespace RSSFilter.Models;

public class TagSplitOptions
{
    public string TagName { get; set; } = string.Empty;
    public string SplitPattern { get; set; } = string.Empty;
    public Dictionary<string, string> NewTags { get; set; } = new Dictionary<string, string>();
}