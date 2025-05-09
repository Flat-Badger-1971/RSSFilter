# RSSFilter

RSSFilter is a .NET application that fetches, filters, and serves customized RSS feeds. It allows you to remove unwanted elements from RSS feeds, split and transform tag content using regular expressions before making the feed available through a web endpoint.

## Features

- **RSS Feed Filtering**: Remove specified XML tags from RSS feeds
- **Tag Splitting**: Split tag content into multiple tags using regex capture groups
- **Content Cleanup**: Clean up feed content using configurable regex patterns
- **Web API**: Access the filtered feed via HTTP endpoints
- **Automatic Updates**: Periodically polls the source feed for updates
- **Configurable**: Easy configuration through appsettings.json
- **Logging**: Built-in file logging with rotation

## Installation

### Requirements

- .NET 8.0 or later

### Setup

1. Clone the repository
2. Build the solution:
   ```
   dotnet build
   ```
3. Run the application:
   ```
   dotnet run --project RSSFilter
   ```

## Configuration

Configuration is managed through the `appsettings.json` file. Here's an overview of the available settings:

### Basic Configuration

| Setting | Description                          | Default |
| ------- | ------------------------------------ | ------- |
| `Port`  | The HTTP port the service listens on | `5000`  |

### RSS Filter Options (`RSSFilter` section)

| Setting        | Description                                    | Default  |
| -------------- | ---------------------------------------------- | -------- |
| `InputSource`  | URL of the source RSS feed                     | Required |
| `TagsToRemove` | Array of XML tag names to remove from the feed | `[ ]`    |
| `CleanupTags`  | Enable/disable tag content processing          | `false`  |
| `TagSplit`     | Array of tag splitting configurations          | `[ ]`    |
| `TagCleanup`   | Array of tag cleanup configurations            | `[ ]`    |

#### Tag Split Settings

Each entry in `TagSplit` contains:

| Setting        | Description                                                           |
| -------------- | --------------------------------------------------------------------- |
| `TagName`      | Name of the XML tag to split                                          |
| `SplitPattern` | Regular expression pattern with two capture groups to extract content |
| `NewTags`      | Dictionary of tag names and patterns using $1 and $2 for replacements |

#### Tag Cleanup Settings

Each entry in `TagCleanup` contains:

| Setting          | Description                                                 |
| ---------------- | ----------------------------------------------------------- |
| `TagName`        | Name of the XML tag to clean up                             |
| `CleanupPattern` | Regular expression pattern to remove from the tag's content |

### Logger Options (`Logger` section)

| Setting            | Description                                    | Default           |
| ------------------ | ---------------------------------------------- | ----------------- |
| `LogDirectory`     | Directory for log files                        | `logs`            |
| `MaxFileSizeBytes` | Maximum size of log files                      | `102400` (100 KB) |
| `BufferSize`       | Number of log entries to buffer before writing | `50`              |

## Example Configuration

```json
{
  "Port": "5000",
  "RSSFilter": {
    "InputSource": "https://example.com/feed.rss",
    "TagsToRemove": [
      "link",
      "tv:data_source",
      "enclosure",
      "media:content",
      "description"
    ],
    "CleanupTags": true,
    "TagSplit": [
      {
        "TagName": "title",
        "SplitPattern": "(.+S\\d{2}(?:E\\d{2})?)\\s(.+)",
        "NewTags": {
          "title": "$1",
          "description": "$2"
        }
      }
    ],
    "TagCleanup": [
      {
        "TagName": "description",
        "CleanupPattern": "\\s(?:\\d{3,4}p)|(?:RERIP).*"
      }
    ]
  },
  "Logger": {
    "LogDirectory": "logs",
    "MaxFileSizeBytes": 102400,
    "BufferSize": 50
  }
}
```

## API Endpoints

| Endpoint   | Method | Description                                        |
| ---------- | ------ | -------------------------------------------------- |
| `/rss`     | GET    | Returns the most recently processed RSS feed       |
| `/refresh` | GET    | Forces an immediate feed update and returns status |

## Use Cases

- Remove unnecessary tags like media enclosures from podcast feeds
- Split episode information into separate tags (title and description)
- Extract season and episode numbers into dedicated tags
- Clean up episode descriptions by removing quality descriptors
- Simplify RSS feeds for better compatibility with basic RSS readers

## License

See the [LICENSE](LICENSE) file for details.
