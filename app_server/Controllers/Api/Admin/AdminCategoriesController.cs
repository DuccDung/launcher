using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using app_server.Contracts.Admin.Categories;
using app_server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace app_server.Controllers.Api.Admin;

[ApiController]
[Authorize(Roles = "ADMIN")]
[Route("api/admin/categories")]
public sealed class AdminCategoriesController(LauncherDbContext dbContext) : ControllerBase
{
    private static readonly Dictionary<string, string> StatusMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["published"] = "Published",
        ["publishes"] = "Published",
        ["publish"] = "Published",
        ["draft"] = "Draft",
        ["hidden"] = "Draft"
    };

    [HttpGet]
    public async Task<ActionResult<AdminCategoryListResponse>> GetAll(CancellationToken cancellationToken)
    {
        var items = await LoadCategoryItemsAsync(cancellationToken);
        return Ok(BuildListResponse(items));
    }

    [HttpGet("{categoryId:guid}")]
    public async Task<ActionResult<AdminCategoryItemResponse>> GetById(Guid categoryId, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .AsNoTracking()
            .Where(item => item.CategoryId == categoryId)
            .Select(item => new CategoryQueryRow(
                item.CategoryId,
                item.Name,
                item.Slug,
                item.Status,
                item.DisplayOrder,
                item.ShortDescription,
                item.GameCategories.Count,
                item.CreatedAt,
                item.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);

        if (category is null)
        {
            return NotFound(new { message = "Không tìm thấy category." });
        }

        return Ok(MapCategory(category));
    }

    [HttpPost]
    public async Task<ActionResult<AdminCategoryItemResponse>> Create(
        [FromBody] AdminCategoryUpsertRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!TryNormalizeStatus(request.Status, out var normalizedStatus))
        {
            ModelState.AddModelError(nameof(request.Status), "Status chỉ hỗ trợ Published hoặc Draft.");
            return ValidationProblem(ModelState);
        }

        var category = new Category
        {
            Name = request.Name.Trim(),
            Slug = await BuildUniqueSlugAsync(request.Slug, request.Name, null, cancellationToken),
            Status = normalizedStatus,
            DisplayOrder = request.DisplayOrder,
            ShortDescription = CleanOptionalText(request.ShortDescription),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { categoryId = category.CategoryId },
            MapCategory(category, 0));
    }

    [HttpPut("{categoryId:guid}")]
    public async Task<ActionResult<AdminCategoryItemResponse>> Update(
        Guid categoryId,
        [FromBody] AdminCategoryUpsertRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!TryNormalizeStatus(request.Status, out var normalizedStatus))
        {
            ModelState.AddModelError(nameof(request.Status), "Status chỉ hỗ trợ Published hoặc Draft.");
            return ValidationProblem(ModelState);
        }

        var category = await dbContext.Categories
            .Include(item => item.GameCategories)
            .SingleOrDefaultAsync(item => item.CategoryId == categoryId, cancellationToken);

        if (category is null)
        {
            return NotFound(new { message = "Không tìm thấy category để cập nhật." });
        }

        category.Name = request.Name.Trim();
        category.Slug = await BuildUniqueSlugAsync(request.Slug, request.Name, categoryId, cancellationToken);
        category.Status = normalizedStatus;
        category.DisplayOrder = request.DisplayOrder;
        category.ShortDescription = CleanOptionalText(request.ShortDescription);
        category.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(MapCategory(category, category.GameCategories.Count));
    }

    [HttpDelete("{categoryId:guid}")]
    public async Task<IActionResult> Delete(Guid categoryId, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.CategoryId == categoryId, cancellationToken);

        if (category is null)
        {
            return NotFound(new { message = "Không tìm thấy category để xóa." });
        }

        var isInUse = await dbContext.GameCategories
            .AsNoTracking()
            .AnyAsync(item => item.CategoryId == categoryId, cancellationToken);

        if (isInUse)
        {
            return Conflict(new { message = "Category đang được gắn với game nên chưa thể xóa." });
        }

        dbContext.Categories.Remove(new Category { CategoryId = categoryId });
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "Đã xóa category thành công.",
            deletedCategoryId = categoryId
        });
    }

    private async Task<List<AdminCategoryItemResponse>> LoadCategoryItemsAsync(CancellationToken cancellationToken)
    {
        var categories = await dbContext.Categories
            .AsNoTracking()
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name)
            .Select(item => new CategoryQueryRow(
                item.CategoryId,
                item.Name,
                item.Slug,
                item.Status,
                item.DisplayOrder,
                item.ShortDescription,
                item.GameCategories.Count,
                item.CreatedAt,
                item.UpdatedAt))
            .ToListAsync(cancellationToken);

        return categories
            .Select(MapCategory)
            .ToList();
    }

    private static AdminCategoryListResponse BuildListResponse(IReadOnlyList<AdminCategoryItemResponse> items)
    {
        var summary = new AdminCategorySummaryResponse(
            TotalCategories: items.Count,
            PublishedCategories: items.Count(item => string.Equals(item.Status, "Published", StringComparison.OrdinalIgnoreCase)),
            DraftCategories: items.Count(item => string.Equals(item.Status, "Draft", StringComparison.OrdinalIgnoreCase)),
            NextDisplayOrder: items.Count == 0 ? 1 : items.Max(item => item.DisplayOrder) + 1,
            LastUpdatedAt: items.Count == 0 ? null : items.Max(item => item.UpdatedAt));

        return new AdminCategoryListResponse(summary, items);
    }

    private static AdminCategoryItemResponse MapCategory(CategoryQueryRow category)
    {
        return MapCategory(
            category.CategoryId,
            category.Name,
            category.Slug,
            category.Status,
            category.DisplayOrder,
            category.ShortDescription,
            category.GameCount,
            category.CreatedAt,
            category.UpdatedAt);
    }

    private static AdminCategoryItemResponse MapCategory(Category category, int gameCount)
    {
        return MapCategory(
            category.CategoryId,
            category.Name,
            category.Slug,
            category.Status,
            category.DisplayOrder,
            category.ShortDescription,
            gameCount,
            category.CreatedAt,
            category.UpdatedAt);
    }

    private static AdminCategoryItemResponse MapCategory(
        Guid categoryId,
        string name,
        string? slug,
        string status,
        int displayOrder,
        string? shortDescription,
        int gameCount,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new AdminCategoryItemResponse(
            categoryId,
            name,
            slug ?? string.Empty,
            NormalizeOutputStatus(status),
            displayOrder,
            shortDescription,
            gameCount,
            createdAt,
            updatedAt);
    }

    private async Task<string> BuildUniqueSlugAsync(
        string? requestedSlug,
        string categoryName,
        Guid? excludedCategoryId,
        CancellationToken cancellationToken)
    {
        var baseSlug = Slugify(string.IsNullOrWhiteSpace(requestedSlug) ? categoryName : requestedSlug);
        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            baseSlug = "category";
        }

        var query = dbContext.Categories
            .AsNoTracking()
            .Where(item => item.Slug != null && EF.Functions.Like(item.Slug, $"{baseSlug}%"));

        if (excludedCategoryId.HasValue)
        {
            query = query.Where(item => item.CategoryId != excludedCategoryId.Value);
        }

        var existingSlugs = await query
            .Select(item => item.Slug!)
            .ToListAsync(cancellationToken);

        if (!existingSlugs.Any(item => string.Equals(item, baseSlug, StringComparison.OrdinalIgnoreCase)))
        {
            return baseSlug;
        }

        var suffix = 2;
        while (existingSlugs.Any(item => string.Equals(item, $"{baseSlug}-{suffix}", StringComparison.OrdinalIgnoreCase)))
        {
            suffix++;
        }

        return $"{baseSlug}-{suffix}";
    }

    private static string CleanOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static bool TryNormalizeStatus(string? status, out string normalizedStatus)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            normalizedStatus = string.Empty;
            return false;
        }

        return StatusMap.TryGetValue(status.Trim(), out normalizedStatus!);
    }

    private static string NormalizeOutputStatus(string? status)
    {
        return TryNormalizeStatus(status, out var normalizedStatus) ? normalizedStatus : "Draft";
    }

    private static string Slugify(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        var cleanedValue = builder.ToString()
            .Normalize(NormalizationForm.FormC)
            .ToLowerInvariant()
            .Replace('đ', 'd');

        cleanedValue = Regex.Replace(cleanedValue, @"[^a-z0-9]+", "-");
        return cleanedValue.Trim('-');
    }

    private sealed record CategoryQueryRow(
        Guid CategoryId,
        string Name,
        string? Slug,
        string Status,
        int DisplayOrder,
        string? ShortDescription,
        int GameCount,
        DateTime CreatedAt,
        DateTime UpdatedAt);
}
