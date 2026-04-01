using System.Text.Json;
using System.Text.Json.Serialization;

namespace app_server.Services.Storefront;

public sealed class SteamStoreAppLookupResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("data")]
    public SteamStoreAppData? Data { get; init; }
}

public sealed class SteamStoreAppData
{
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("steam_appid")]
    public int SteamAppId { get; init; }

    [JsonPropertyName("required_age")]
    public int RequiredAge { get; init; }

    [JsonPropertyName("is_free")]
    public bool IsFree { get; init; }

    [JsonPropertyName("dlc")]
    public List<int>? Dlc { get; init; }

    [JsonPropertyName("detailed_description")]
    public string? DetailedDescription { get; init; }

    [JsonPropertyName("about_the_game")]
    public string? AboutTheGame { get; init; }

    [JsonPropertyName("short_description")]
    public string? ShortDescription { get; init; }

    [JsonPropertyName("supported_languages")]
    public string? SupportedLanguages { get; init; }

    [JsonPropertyName("reviews")]
    public string? Reviews { get; init; }

    [JsonPropertyName("header_image")]
    public string? HeaderImage { get; init; }

    [JsonPropertyName("capsule_image")]
    public string? CapsuleImage { get; init; }

    [JsonPropertyName("capsule_imagev5")]
    public string? CapsuleImageV5 { get; init; }

    [JsonPropertyName("website")]
    public string? Website { get; init; }

    [JsonPropertyName("pc_requirements")]
    public SteamStoreRequirements? PcRequirements { get; init; }

    [JsonPropertyName("mac_requirements")]
    public SteamStoreRequirements? MacRequirements { get; init; }

    [JsonPropertyName("linux_requirements")]
    public SteamStoreRequirements? LinuxRequirements { get; init; }

    [JsonPropertyName("developers")]
    public List<string>? Developers { get; init; }

    [JsonPropertyName("publishers")]
    public List<string>? Publishers { get; init; }

    [JsonPropertyName("platforms")]
    public SteamStorePlatforms? Platforms { get; init; }

    [JsonPropertyName("metacritic")]
    public SteamStoreMetacritic? Metacritic { get; init; }

    [JsonPropertyName("categories")]
    public List<SteamStoreNamedItem>? Categories { get; init; }

    [JsonPropertyName("genres")]
    public List<SteamStoreNamedItem>? Genres { get; init; }

    [JsonPropertyName("screenshots")]
    public List<SteamStoreScreenshot>? Screenshots { get; init; }

    [JsonPropertyName("movies")]
    public List<SteamStoreMovie>? Movies { get; init; }

    [JsonPropertyName("recommendations")]
    public SteamStoreRecommendations? Recommendations { get; init; }

    [JsonPropertyName("release_date")]
    public SteamStoreReleaseDate? ReleaseDate { get; init; }

    [JsonPropertyName("support_info")]
    public SteamStoreSupportInfo? SupportInfo { get; init; }

    [JsonPropertyName("background")]
    public string? Background { get; init; }

    [JsonPropertyName("background_raw")]
    public string? BackgroundRaw { get; init; }

    [JsonPropertyName("content_descriptors")]
    public SteamStoreContentDescriptors? ContentDescriptors { get; init; }

    [JsonPropertyName("ratings")]
    public Dictionary<string, SteamStoreRating>? Ratings { get; init; }
}

public sealed class SteamStoreRequirements
{
    [JsonPropertyName("minimum")]
    public string? Minimum { get; init; }

    [JsonPropertyName("recommended")]
    public string? Recommended { get; init; }
}

public sealed class SteamStorePlatforms
{
    [JsonPropertyName("windows")]
    public bool Windows { get; init; }

    [JsonPropertyName("mac")]
    public bool Mac { get; init; }

    [JsonPropertyName("linux")]
    public bool Linux { get; init; }
}

public sealed class SteamStoreMetacritic
{
    [JsonPropertyName("score")]
    public int? Score { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }
}

public sealed class SteamStoreNamedItem
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? Id { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }
}

public sealed class SteamStoreScreenshot
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("path_thumbnail")]
    public string? Thumbnail { get; init; }

    [JsonPropertyName("path_full")]
    public string? Full { get; init; }
}

public sealed class SteamStoreMovie
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; init; }
}

public sealed class SteamStoreRecommendations
{
    [JsonPropertyName("total")]
    public int Total { get; init; }
}

public sealed class SteamStoreReleaseDate
{
    [JsonPropertyName("coming_soon")]
    public bool ComingSoon { get; init; }

    [JsonPropertyName("date")]
    public string? Date { get; init; }
}

public sealed class SteamStoreSupportInfo
{
    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }
}

public sealed class SteamStoreContentDescriptors
{
    [JsonPropertyName("ids")]
    public List<int>? Ids { get; init; }

    [JsonPropertyName("notes")]
    public string? Notes { get; init; }
}

public sealed class SteamStoreRating
{
    [JsonPropertyName("rating")]
    public string? Rating { get; init; }

    [JsonPropertyName("descriptors")]
    public string? Descriptors { get; init; }
}

public sealed class FlexibleStringJsonConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.TryGetInt64(out var longValue)
                ? longValue.ToString()
                : reader.GetDouble().ToString(System.Globalization.CultureInfo.InvariantCulture),
            JsonTokenType.True => bool.TrueString,
            JsonTokenType.False => bool.FalseString,
            JsonTokenType.Null => null,
            _ => JsonDocument.ParseValue(ref reader).RootElement.ToString()
        };
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value);
    }
}
