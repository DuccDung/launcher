using System.Text.Json;
using app_server.Models;
using app_server.ViewModels.Storefront;

namespace app_server.Services.Storefront;

public static class StorefrontArticleMapper
{
    public static ProductArticleViewModel? Build(Game game, Article? article)
    {
        if (article is null)
        {
            return null;
        }

        string? eyebrow = null;
        string? title = null;
        string? summaryFromJson = null;
        var blocks = new List<ProductArticleBlockViewModel>();

        if (!string.IsNullOrWhiteSpace(article.ContentJson))
        {
            try
            {
                using var document = JsonDocument.Parse(article.ContentJson);
                var root = document.RootElement;

                eyebrow = ReadString(root, "eyebrow");
                title = ReadString(root, "title");
                summaryFromJson = ReadString(root, "summary");

                if (root.TryGetProperty("blocks", out var blocksElement) &&
                    blocksElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var block in blocksElement.EnumerateArray())
                    {
                        var mapped = MapBlock(block);
                        if (mapped is not null)
                        {
                            blocks.Add(mapped);
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Keep rendering the rest of the page even if article JSON is malformed.
            }
        }

        var summary = FirstNonEmpty(article.Summary, summaryFromJson);
        var resolvedTitle = FirstNonEmpty(title, game.Name, "Bài viết game");
        var hasContent = !string.IsNullOrWhiteSpace(eyebrow) ||
                         !string.IsNullOrWhiteSpace(summary) ||
                         blocks.Count > 0 ||
                         !string.IsNullOrWhiteSpace(article.ContentJson);

        if (!hasContent)
        {
            return null;
        }

        return new ProductArticleViewModel
        {
            Eyebrow = eyebrow,
            Title = resolvedTitle!,
            Summary = summary,
            Blocks = blocks
        };
    }

    private static ProductArticleBlockViewModel? MapBlock(JsonElement block)
    {
        var type = ReadString(block, "type")?.ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(type))
        {
            type = "paragraph";
        }

        return type switch
        {
            "heading" => BuildTextBlock(type, ReadString(block, "text")),
            "paragraph" => BuildTextBlock(type, ReadString(block, "text")),
            "quote" => BuildTextBlock(type, ReadString(block, "text")),
            "image" => BuildImageBlock(block),
            "video" => BuildVideoBlock(block),
            "list" => BuildListBlock(block),
            _ => BuildTextBlock("paragraph", ReadString(block, "text"))
        };
    }

    private static ProductArticleBlockViewModel? BuildTextBlock(string type, string? text)
    {
        text = Clean(text);
        return string.IsNullOrWhiteSpace(text)
            ? null
            : new ProductArticleBlockViewModel
            {
                Type = type,
                Text = text
            };
    }

    private static ProductArticleBlockViewModel? BuildImageBlock(JsonElement block)
    {
        var url = Clean(ReadString(block, "url"));
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        return new ProductArticleBlockViewModel
        {
            Type = "image",
            Url = url,
            Alt = Clean(ReadString(block, "alt"))
        };
    }

    private static ProductArticleBlockViewModel? BuildVideoBlock(JsonElement block)
    {
        var url = Clean(ReadString(block, "url"));
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var isYoutube = IsYoutube(url);
        return new ProductArticleBlockViewModel
        {
            Type = "video",
            Url = url,
            Title = Clean(ReadString(block, "title")),
            IsYoutube = isYoutube,
            VideoEmbedUrl = isYoutube ? ToYoutubeEmbed(url) : null
        };
    }

    private static ProductArticleBlockViewModel? BuildListBlock(JsonElement block)
    {
        var items = new List<string>();
        if (block.TryGetProperty("items", out var itemsElement))
        {
            if (itemsElement.ValueKind == JsonValueKind.Array)
            {
                items.AddRange(itemsElement.EnumerateArray()
                    .Select(item => Clean(item.ValueKind == JsonValueKind.String ? item.GetString() : item.ToString()))
                    .Where(item => !string.IsNullOrWhiteSpace(item))!);
            }
            else if (itemsElement.ValueKind == JsonValueKind.String)
            {
                items.AddRange(itemsElement
                    .GetString()!
                    .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(Clean)
                    .Where(item => !string.IsNullOrWhiteSpace(item))!);
            }
        }

        var intro = Clean(ReadString(block, "intro"));
        return items.Count == 0 && string.IsNullOrWhiteSpace(intro)
            ? null
            : new ProductArticleBlockViewModel
            {
                Type = "list",
                Intro = intro,
                Items = items
            };
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.ToString(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            _ => null
        };
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
    }

    private static bool IsYoutube(string value)
    {
        return value.Contains("youtu.be", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("youtube.com", StringComparison.OrdinalIgnoreCase);
    }

    private static string ToYoutubeEmbed(string value)
    {
        try
        {
            var uri = new Uri(value);
            var id = uri.Host.Contains("youtu.be", StringComparison.OrdinalIgnoreCase)
                ? uri.AbsolutePath.Trim('/')
                : Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query).TryGetValue("v", out var queryValue)
                    ? queryValue.ToString()
                    : string.Empty;

            return string.IsNullOrWhiteSpace(id)
                ? value
                : $"https://www.youtube-nocookie.com/embed/{id}?rel=0";
        }
        catch
        {
            return value;
        }
    }
}
