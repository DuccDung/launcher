using System.ComponentModel.DataAnnotations;

namespace app_server.Contracts.Admin.GameWorkspace;

public sealed class AdminGameUpsertRequest
{
    [Required(ErrorMessage = "Ten game la bat buoc.")]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Steam App ID phai lon hon 0.")]
    public int? SteamAppId { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Steam price phai lon hon hoac bang 0.")]
    public decimal? SteamPrice { get; set; }

    [StringLength(500)]
    public string? PhotoUrl { get; set; }

    public bool IsRemove { get; set; }

    public List<Guid> CategoryIds { get; set; } = new();
}

public sealed class AdminGameTrendingUpdateRequest
{
    public bool IsTrending { get; set; }
}

public sealed class AdminGameVersionUpsertRequest
{
    [StringLength(255)]
    public string? VersionName { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Gia version phai lon hon hoac bang 0.")]
    public decimal? Price { get; set; }

    public bool IsRemoved { get; set; }
}

public sealed class AdminMediaUpsertRequest
{
    [StringLength(100)]
    public string? MediaType { get; set; }

    [Required]
    [StringLength(1000)]
    public string Url { get; set; } = string.Empty;
}

public sealed record AdminSteamPreviewResponse(
    int SteamAppId,
    string Name,
    string? PhotoUrl,
    IReadOnlyList<string> Tags,
    string? ReleaseDate,
    decimal OriginalPrice,
    decimal SalePrice,
    string OriginalPriceText,
    string SalePriceText,
    bool IsFree);

public sealed record AdminWorkspaceStatsResponse(
    int TotalGames,
    int TotalVersions,
    int TotalMedia);

public sealed record AdminWorkspaceCategoryResponse(
    Guid CategoryId,
    string Name,
    int DisplayOrder,
    string? ShortDescription);

public sealed record AdminWorkspaceGameVersionListItemResponse(
    Guid VersionId,
    string? VersionName,
    decimal? Price,
    bool IsRemoved);

public sealed record AdminWorkspaceGameListItemResponse(
    Guid GameId,
    string Name,
    string Slug,
    int? SteamAppId,
    decimal? SteamPrice,
    string? PhotoUrl,
    bool IsTrending,
    bool IsRemove,
    IReadOnlyList<Guid> CategoryIds,
    IReadOnlyList<string> CategoryNames,
    IReadOnlyList<AdminWorkspaceGameVersionListItemResponse> Versions,
    int VersionCount,
    int MediaCount,
    DateTime UpdatedAt);

public sealed record AdminWorkspaceGameResponse(
    Guid GameId,
    string Name,
    string Slug,
    int? SteamAppId,
    decimal? SteamPrice,
    string? PhotoUrl,
    bool IsTrending,
    bool IsRemove,
    IReadOnlyList<Guid> CategoryIds,
    DateTime UpdatedAt);

public sealed record AdminWorkspaceVersionResponse(
    Guid VersionId,
    Guid GameId,
    string? VersionName,
    decimal? Price,
    bool IsRemoved,
    DateTime UpdatedAt);

public sealed record AdminWorkspaceMediaResponse(
    Guid MediaId,
    Guid GameId,
    string Url,
    string? MediaType,
    DateTime UpdatedAt);

public sealed record AdminWorkspaceBootstrapResponse(
    AdminWorkspaceStatsResponse Stats,
    IReadOnlyList<AdminWorkspaceCategoryResponse> Categories,
    IReadOnlyList<AdminWorkspaceGameListItemResponse> Games);

public sealed record AdminWorkspaceDetailsResponse(
    AdminWorkspaceGameResponse Game,
    IReadOnlyList<AdminWorkspaceVersionResponse> Versions,
    IReadOnlyList<AdminWorkspaceMediaResponse> MediaItems);

public sealed record AdminWorkspaceUploadResponse(
    string Url,
    string FileName,
    string ContentType,
    long Size);
