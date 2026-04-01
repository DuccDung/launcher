using System.ComponentModel.DataAnnotations;

namespace app_server.Contracts.Admin.GameWorkspace;

public sealed class AdminGameUpsertRequest
{
    [Required(ErrorMessage = "Tên game là bắt buộc.")]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Steam App ID phải lớn hơn 0.")]
    public int? SteamAppId { get; set; }

    [Required(ErrorMessage = "Rating là bắt buộc.")]
    [Range(typeof(decimal), "0", "5", ErrorMessage = "Rating phải nằm trong khoảng từ 0 đến 5.")]
    public decimal? Rating { get; set; }

    [Required(ErrorMessage = "Giá cũ là bắt buộc.")]
    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Giá cũ phải lớn hơn hoặc bằng 0.")]
    public decimal? OldPrice { get; set; }

    [Required(ErrorMessage = "Giá mới là bắt buộc.")]
    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Giá mới phải lớn hơn hoặc bằng 0.")]
    public decimal? NewPrice { get; set; }

    public List<Guid> CategoryIds { get; set; } = new();
}

public sealed class AdminGameVersionUpsertRequest
{
    [StringLength(255)]
    public string? VersionName { get; set; }

    public bool IsRemoved { get; set; }
}

public sealed class AdminAccountUpsertRequest
{
    public Guid? VersionId { get; set; }

    public bool IsActive { get; set; }

    public bool IsPurchased { get; set; }
}

public sealed class AdminGameFileUpsertRequest
{
    [Required]
    public Guid AccountId { get; set; }

    [StringLength(100)]
    public string? FileType { get; set; }

    public bool IsActive { get; set; }

    [StringLength(1000)]
    public string? FileUrl01 { get; set; }

    [StringLength(1000)]
    public string? FileUrl02 { get; set; }

    [StringLength(1000)]
    public string? FileUrl03 { get; set; }

    [StringLength(1000)]
    public string? FileUrl04 { get; set; }

    [StringLength(1000)]
    public string? FileUrl05 { get; set; }
}

public sealed class AdminMediaUpsertRequest
{
    [StringLength(100)]
    public string? MediaType { get; set; }

    [Required]
    [StringLength(1000)]
    public string Url { get; set; } = string.Empty;
}

public sealed class AdminArticleUpsertRequest
{
    public string? Summary { get; set; }

    [Required]
    public string ContentJson { get; set; } = string.Empty;
}

public sealed record AdminWorkspaceStatsResponse(
    int TotalGames,
    int TotalVersions,
    int TotalAccounts,
    int TotalFiles,
    int TotalMedia,
    int TotalArticles);

public sealed record AdminWorkspaceCategoryResponse(
    Guid CategoryId,
    string Name,
    int DisplayOrder,
    string? ShortDescription);

public sealed record AdminWorkspaceGameListItemResponse(
    Guid GameId,
    string Name,
    string Slug,
    int? SteamAppId,
    decimal? Rating,
    decimal? OldPrice,
    decimal? NewPrice,
    IReadOnlyList<Guid> CategoryIds,
    IReadOnlyList<string> CategoryNames,
    int VersionCount,
    int MediaCount,
    bool HasArticle,
    DateTime UpdatedAt);

public sealed record AdminWorkspaceGameResponse(
    Guid GameId,
    string Name,
    string Slug,
    int? SteamAppId,
    decimal? Rating,
    decimal? OldPrice,
    decimal? NewPrice,
    IReadOnlyList<Guid> CategoryIds,
    DateTime UpdatedAt);

public sealed record AdminWorkspaceVersionResponse(
    Guid VersionId,
    Guid GameId,
    string? VersionName,
    int LinkedAccountCount,
    bool IsRemoved,
    DateTime UpdatedAt);

public sealed record AdminWorkspaceAccountResponse(
    Guid AccountId,
    Guid? VersionId,
    bool IsActive,
    bool IsPurchased,
    int GameFileCount,
    DateTime UpdatedAt);

public sealed record AdminWorkspaceFileResponse(
    Guid FileId,
    Guid AccountId,
    string? FileType,
    bool IsActive,
    string? FileUrl01,
    string? FileUrl02,
    string? FileUrl03,
    string? FileUrl04,
    string? FileUrl05,
    DateTime UpdatedAt);

public sealed record AdminWorkspaceMediaResponse(
    Guid MediaId,
    Guid GameId,
    string Url,
    string? MediaType,
    DateTime UpdatedAt);

public sealed record AdminWorkspaceArticleResponse(
    Guid ArticleId,
    Guid GameId,
    string? Summary,
    string ContentJson,
    DateTime UpdatedAt);

public sealed record AdminWorkspaceBootstrapResponse(
    AdminWorkspaceStatsResponse Stats,
    IReadOnlyList<AdminWorkspaceCategoryResponse> Categories,
    IReadOnlyList<AdminWorkspaceGameListItemResponse> Games,
    IReadOnlyList<AdminWorkspaceAccountResponse> Accounts);

public sealed record AdminWorkspaceDetailsResponse(
    AdminWorkspaceGameResponse Game,
    IReadOnlyList<AdminWorkspaceVersionResponse> Versions,
    IReadOnlyList<AdminWorkspaceFileResponse> Files,
    IReadOnlyList<AdminWorkspaceMediaResponse> MediaItems,
    AdminWorkspaceArticleResponse? Article);

public sealed record AdminWorkspaceUploadResponse(
    string Url,
    string FileName,
    string ContentType,
    long Size);
