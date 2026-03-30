using System.ComponentModel.DataAnnotations;

namespace app_server.Contracts.Admin.Categories;

public sealed class AdminCategoryUpsertRequest
{
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Slug { get; set; }

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Published";

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; }

    [StringLength(1000)]
    public string? ShortDescription { get; set; }
}

public sealed record AdminCategoryItemResponse(
    Guid CategoryId,
    string Name,
    string Slug,
    string Status,
    int DisplayOrder,
    string? ShortDescription,
    int GameCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record AdminCategorySummaryResponse(
    int TotalCategories,
    int PublishedCategories,
    int DraftCategories,
    int NextDisplayOrder,
    DateTime? LastUpdatedAt);

public sealed record AdminCategoryListResponse(
    AdminCategorySummaryResponse Summary,
    IReadOnlyList<AdminCategoryItemResponse> Items);
