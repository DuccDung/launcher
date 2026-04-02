using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using app_server.Contracts.Admin.GameWorkspace;
using app_server.Models;
using app_server.Services.Storefront;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;

namespace app_server.Controllers.Api.Admin;

[ApiController]
[Authorize(Roles = "ADMIN")]
[Route("api/admin/game-workspace")]
public sealed class AdminGameWorkspaceController(
    LauncherDbContext dbContext,
    IWebHostEnvironment webHostEnvironment,
    ISteamStoreService steamStoreService) : ControllerBase
{
    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".webp", ".gif", ".bmp"];

    [HttpGet("bootstrap")]
    public async Task<ActionResult<AdminWorkspaceBootstrapResponse>> GetBootstrap(CancellationToken cancellationToken)
    {
        return Ok(await BuildBootstrapResponseAsync(cancellationToken));
    }

    [HttpGet("steam-preview/{steamAppId:int}")]
    public async Task<ActionResult<AdminSteamPreviewResponse>> GetSteamPreview(int steamAppId, CancellationToken cancellationToken)
    {
        var steamData = await steamStoreService.GetAppDetailsAsync(steamAppId, cancellationToken);
        if (steamData is null || string.IsNullOrWhiteSpace(steamData.Name))
        {
            return NotFound(new { message = "Khong tim thay du lieu tu Steam cho app id nay." });
        }

        var originalPrice = NormalizeSteamAmount(steamData.PriceOverview?.Initial);
        var salePrice = NormalizeSteamAmount(steamData.PriceOverview?.Final);
        if (salePrice <= 0 && originalPrice > 0)
        {
            salePrice = originalPrice;
        }

        var tags = BuildSteamTags(steamData);
        return Ok(new AdminSteamPreviewResponse(
            SteamAppId: steamData.SteamAppId,
            Name: steamData.Name.Trim(),
            PhotoUrl: steamData.HeaderImage,
            Tags: tags,
            ReleaseDate: steamData.ReleaseDate?.Date,
            OriginalPrice: originalPrice,
            SalePrice: salePrice,
            OriginalPriceText: steamData.IsFree ? "Mien phi" : FormatVnd(originalPrice),
            SalePriceText: steamData.IsFree ? "Mien phi" : FormatVnd(salePrice),
            IsFree: steamData.IsFree));
    }

    [HttpGet("games/{gameId:guid}")]
    public async Task<ActionResult<AdminWorkspaceDetailsResponse>> GetGameWorkspace(Guid gameId, CancellationToken cancellationToken)
    {
        var response = await BuildGameWorkspaceResponseAsync(gameId, cancellationToken);
        return response is null
            ? NotFound(new { message = "Khong tim thay game workspace." })
            : Ok(response);
    }

    [HttpPost("games")]
    public async Task<IActionResult> CreateGame([FromBody] AdminGameUpsertRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            ModelState.AddModelError(nameof(request.Name), "Ten game la bat buoc.");
            return ValidationProblem(ModelState);
        }

        var validCategoryIds = await LoadValidCategoryIdsAsync(request.CategoryIds, cancellationToken);
        if (validCategoryIds.Count == 0)
        {
            ModelState.AddModelError(nameof(request.CategoryIds), "Moi game phai chon it nhat mot category hop le.");
            return ValidationProblem(ModelState);
        }

        var timestamp = DateTime.UtcNow;
        var game = new Game
        {
            Name = request.Name.Trim(),
            SteamAppId = request.SteamAppId,
            Rating = request.Rating,
            SteamPrice = request.SteamPrice,
            PhotoUrl = CleanOptionalText(request.PhotoUrl),
            IsRemove = request.IsRemove,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };

        dbContext.Games.Add(game);
        await dbContext.SaveChangesAsync(cancellationToken);
        await SyncGameCategoriesAsync(game.GameId, validCategoryIds, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "Da tao game moi.",
            gameId = game.GameId
        });
    }

    [HttpPut("games/{gameId:guid}")]
    public async Task<IActionResult> UpdateGame(Guid gameId, [FromBody] AdminGameUpsertRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var game = await dbContext.Games.SingleOrDefaultAsync(item => item.GameId == gameId, cancellationToken);
        if (game is null)
        {
            return NotFound(new { message = "Khong tim thay game de cap nhat." });
        }

        var validCategoryIds = await LoadValidCategoryIdsAsync(request.CategoryIds, cancellationToken);
        if (validCategoryIds.Count == 0)
        {
            ModelState.AddModelError(nameof(request.CategoryIds), "Moi game phai chon it nhat mot category hop le.");
            return ValidationProblem(ModelState);
        }

        game.Name = request.Name.Trim();
        game.SteamAppId = request.SteamAppId;
        game.Rating = request.Rating;
        game.SteamPrice = request.SteamPrice;
        game.PhotoUrl = CleanOptionalText(request.PhotoUrl);
        game.IsRemove = request.IsRemove;
        game.UpdatedAt = DateTime.UtcNow;

        await SyncGameCategoriesAsync(game.GameId, validCategoryIds, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Da cap nhat game." });
    }

    [HttpDelete("games/{gameId:guid}")]
    public async Task<IActionResult> DeleteGame(Guid gameId, CancellationToken cancellationToken)
    {
        var game = await dbContext.Games.SingleOrDefaultAsync(item => item.GameId == gameId, cancellationToken);
        if (game is null)
        {
            return NotFound(new { message = "Khong tim thay game de xoa." });
        }

        var timestamp = DateTime.UtcNow;
        game.IsRemove = true;
        game.UpdatedAt = timestamp;

        var versions = await dbContext.GameVersions
            .Where(item => item.GameId == gameId)
            .ToListAsync(cancellationToken);

        foreach (var version in versions)
        {
            version.IsRemoved = true;
            version.UpdatedAt = timestamp;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Da an game khoi store." });
    }

    [HttpPost("games/{gameId:guid}/versions")]
    public async Task<IActionResult> CreateVersion(Guid gameId, [FromBody] AdminGameVersionUpsertRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (string.IsNullOrWhiteSpace(request.VersionName))
        {
            return BadRequest(new { message = "Version name la bat buoc." });
        }

        if (!await dbContext.Games.AnyAsync(item => item.GameId == gameId, cancellationToken))
        {
            return NotFound(new { message = "Khong tim thay game de tao version." });
        }

        var timestamp = DateTime.UtcNow;
        var version = new GameVersion
        {
            GameId = gameId,
            VersionName = CleanOptionalText(request.VersionName),
            Price = request.Price,
            IsRemoved = request.IsRemoved,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };

        dbContext.GameVersions.Add(version);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Da tao version moi.", versionId = version.VersionId });
    }

    [HttpPut("versions/{versionId:guid}")]
    public async Task<IActionResult> UpdateVersion(Guid versionId, [FromBody] AdminGameVersionUpsertRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (string.IsNullOrWhiteSpace(request.VersionName))
        {
            return BadRequest(new { message = "Version name la bat buoc." });
        }

        var version = await dbContext.GameVersions.SingleOrDefaultAsync(item => item.VersionId == versionId, cancellationToken);
        if (version is null)
        {
            return NotFound(new { message = "Khong tim thay version de cap nhat." });
        }

        version.VersionName = CleanOptionalText(request.VersionName);
        version.Price = request.Price;
        version.IsRemoved = request.IsRemoved;
        version.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Da cap nhat version." });
    }

    [HttpDelete("versions/{versionId:guid}")]
    public async Task<IActionResult> DeleteVersion(Guid versionId, CancellationToken cancellationToken)
    {
        var version = await dbContext.GameVersions.SingleOrDefaultAsync(item => item.VersionId == versionId, cancellationToken);
        if (version is null)
        {
            return NotFound(new { message = "Khong tim thay version de xoa." });
        }

        version.IsRemoved = true;
        version.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Da an version." });
    }

    [HttpPost("games/{gameId:guid}/media")]
    public async Task<IActionResult> CreateMedia(Guid gameId, [FromBody] AdminMediaUpsertRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!await dbContext.Games.AnyAsync(item => item.GameId == gameId, cancellationToken))
        {
            return NotFound(new { message = "Khong tim thay game de them media." });
        }

        var timestamp = DateTime.UtcNow;
        var media = new Media
        {
            GameId = gameId,
            MediaType = CleanOptionalText(request.MediaType),
            Url = request.Url.Trim(),
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };

        dbContext.Media.Add(media);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Da tao media moi.", mediaId = media.MediaId });
    }

    [HttpPut("media/{mediaId:guid}")]
    public async Task<IActionResult> UpdateMedia(Guid mediaId, [FromBody] AdminMediaUpsertRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var media = await dbContext.Media.SingleOrDefaultAsync(item => item.MediaId == mediaId, cancellationToken);
        if (media is null)
        {
            return NotFound(new { message = "Khong tim thay media de cap nhat." });
        }

        media.MediaType = CleanOptionalText(request.MediaType);
        media.Url = request.Url.Trim();
        media.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Da cap nhat media." });
    }

    [HttpDelete("media/{mediaId:guid}")]
    public async Task<IActionResult> DeleteMedia(Guid mediaId, CancellationToken cancellationToken)
    {
        var media = await dbContext.Media.SingleOrDefaultAsync(item => item.MediaId == mediaId, cancellationToken);
        if (media is null)
        {
            return NotFound(new { message = "Khong tim thay media de xoa." });
        }

        dbContext.Media.Remove(media);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Da xoa media." });
    }

    [HttpPost("uploads/image")]
    public Task<ActionResult<AdminWorkspaceUploadResponse>> UploadImage([FromForm] IFormFile? file, CancellationToken cancellationToken)
    {
        return SaveUploadAsync(file, "images", true, cancellationToken);
    }

    [HttpPost("uploads/file")]
    public Task<ActionResult<AdminWorkspaceUploadResponse>> UploadFile([FromForm] IFormFile? file, CancellationToken cancellationToken)
    {
        return SaveUploadAsync(file, "files", false, cancellationToken);
    }

    [HttpGet("uploads/{bucket}/{fileName}")]
    public IActionResult GetUpload(string bucket, string fileName)
    {
        var normalizedBucket = NormalizeBucket(bucket);
        if (normalizedBucket is null)
        {
            return NotFound();
        }

        var safeFileName = Path.GetFileName(fileName);
        var filePath = Path.Combine(GetUploadDirectory(normalizedBucket), safeFileName);
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound();
        }

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(filePath, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        return PhysicalFile(filePath, contentType);
    }

    private async Task<ActionResult<AdminWorkspaceUploadResponse>> SaveUploadAsync(
        IFormFile? file,
        string bucket,
        bool mustBeImage,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Khong co file nao duoc gui len." });
        }

        var extension = Path.GetExtension(file.FileName);
        if (mustBeImage && !ImageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "File anh chi ho tro png, jpg, jpeg, webp, gif, bmp." });
        }

        var safeExtension = string.IsNullOrWhiteSpace(extension) ? ".bin" : extension.ToLowerInvariant();
        var storedName = $"{Guid.NewGuid():N}{safeExtension}";
        var uploadDirectory = GetUploadDirectory(bucket);
        Directory.CreateDirectory(uploadDirectory);

        var filePath = Path.Combine(uploadDirectory, storedName);
        await using (var stream = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var url = Url.Action(nameof(GetUpload), values: new { bucket, fileName = storedName }) ??
                  $"/api/admin/game-workspace/uploads/{bucket}/{storedName}";

        return Ok(new AdminWorkspaceUploadResponse(
            Url: url,
            FileName: storedName,
            ContentType: file.ContentType,
            Size: file.Length));
    }

    private async Task<AdminWorkspaceBootstrapResponse> BuildBootstrapResponseAsync(CancellationToken cancellationToken)
    {
        var categories = await dbContext.Categories
            .AsNoTracking()
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name)
            .Select(item => new AdminWorkspaceCategoryResponse(
                item.CategoryId,
                item.Name,
                item.DisplayOrder,
                item.ShortDescription))
            .ToListAsync(cancellationToken);

        var games = await dbContext.Games
            .AsNoTracking()
            .Where(item => !item.IsRemove)
            .OrderByDescending(item => item.UpdatedAt)
            .ThenBy(item => item.Name)
            .Select(item => new
            {
                item.GameId,
                item.Name,
                item.SteamAppId,
                item.Rating,
                item.SteamPrice,
                item.PhotoUrl,
                item.IsRemove,
                item.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        var gameIds = games.Select(item => item.GameId).ToList();
        var gameCategoryLinks = await dbContext.GameCategories
            .AsNoTracking()
            .Where(item => gameIds.Contains(item.GameId))
            .Join(
                dbContext.Categories.AsNoTracking(),
                link => link.CategoryId,
                category => category.CategoryId,
                (link, category) => new { link.GameId, category.CategoryId, category.Name })
            .ToListAsync(cancellationToken);

        var versionCounts = await dbContext.GameVersions
            .AsNoTracking()
            .Where(item => gameIds.Contains(item.GameId) && !item.IsRemoved)
            .GroupBy(item => item.GameId)
            .Select(group => new { GameId = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var mediaCounts = await dbContext.Media
            .AsNoTracking()
            .Where(item => item.GameId.HasValue && gameIds.Contains(item.GameId.Value))
            .GroupBy(item => item.GameId!.Value)
            .Select(group => new { GameId = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var versionCountMap = versionCounts.ToDictionary(item => item.GameId, item => item.Count);
        var mediaCountMap = mediaCounts.ToDictionary(item => item.GameId, item => item.Count);
        var categoryMap = gameCategoryLinks
            .GroupBy(item => item.GameId)
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    CategoryIds = (IReadOnlyList<Guid>)group.Select(item => item.CategoryId).Distinct().ToList(),
                    CategoryNames = (IReadOnlyList<string>)group.Select(item => item.Name).Distinct().OrderBy(item => item).ToList()
                });

        var gameItems = games
            .Select(item =>
            {
                var categoriesForGame = categoryMap.GetValueOrDefault(item.GameId);

                return new AdminWorkspaceGameListItemResponse(
                    item.GameId,
                    item.Name,
                    Slugify(item.Name),
                    item.SteamAppId,
                    item.Rating,
                    item.SteamPrice,
                    item.PhotoUrl,
                    item.IsRemove,
                    categoriesForGame?.CategoryIds ?? Array.Empty<Guid>(),
                    categoriesForGame?.CategoryNames ?? Array.Empty<string>(),
                    versionCountMap.GetValueOrDefault(item.GameId, 0),
                    mediaCountMap.GetValueOrDefault(item.GameId, 0),
                    item.UpdatedAt);
            })
            .ToList();

        var stats = new AdminWorkspaceStatsResponse(
            TotalGames: games.Count,
            TotalVersions: await dbContext.GameVersions
                .AsNoTracking()
                .Where(item => !item.IsRemoved && gameIds.Contains(item.GameId))
                .CountAsync(cancellationToken),
            TotalMedia: await dbContext.Media
                .AsNoTracking()
                .Where(item => item.GameId.HasValue && gameIds.Contains(item.GameId.Value))
                .CountAsync(cancellationToken));

        return new AdminWorkspaceBootstrapResponse(stats, categories, gameItems);
    }

    private async Task<AdminWorkspaceDetailsResponse?> BuildGameWorkspaceResponseAsync(Guid gameId, CancellationToken cancellationToken)
    {
        var game = await dbContext.Games
            .AsNoTracking()
            .Where(item => item.GameId == gameId && !item.IsRemove)
            .Select(item => new
            {
                item.GameId,
                item.Name,
                item.SteamAppId,
                item.Rating,
                item.SteamPrice,
                item.PhotoUrl,
                item.IsRemove,
                item.UpdatedAt
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (game is null)
        {
            return null;
        }

        var categoryIds = await dbContext.GameCategories
            .AsNoTracking()
            .Where(item => item.GameId == gameId)
            .OrderBy(item => item.CreatedAt)
            .Select(item => item.CategoryId)
            .ToListAsync(cancellationToken);

        var versions = await dbContext.GameVersions
            .AsNoTracking()
            .Where(item => item.GameId == gameId)
            .OrderBy(item => item.CreatedAt)
            .ThenBy(item => item.VersionName)
            .Select(item => new AdminWorkspaceVersionResponse(
                item.VersionId,
                item.GameId,
                item.VersionName,
                item.Price,
                item.IsRemoved,
                item.UpdatedAt))
            .ToListAsync(cancellationToken);

        var mediaItems = await dbContext.Media
            .AsNoTracking()
            .Where(item => item.GameId == gameId)
            .OrderByDescending(item => item.UpdatedAt)
            .Select(item => new AdminWorkspaceMediaResponse(
                item.MediaId,
                item.GameId ?? gameId,
                item.Url,
                item.MediaType,
                item.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new AdminWorkspaceDetailsResponse(
            new AdminWorkspaceGameResponse(
                game.GameId,
                game.Name,
                Slugify(game.Name),
                game.SteamAppId,
                game.Rating,
                game.SteamPrice,
                game.PhotoUrl,
                game.IsRemove,
                categoryIds,
                game.UpdatedAt),
            versions,
            mediaItems);
    }

    private async Task SyncGameCategoriesAsync(Guid gameId, IEnumerable<Guid> categoryIds, CancellationToken cancellationToken)
    {
        var validCategoryIds = categoryIds
            .Where(item => item != Guid.Empty)
            .Distinct()
            .ToList();

        var currentLinks = await dbContext.GameCategories
            .Where(item => item.GameId == gameId)
            .ToListAsync(cancellationToken);

        var toRemove = currentLinks.Where(item => !validCategoryIds.Contains(item.CategoryId)).ToList();
        dbContext.GameCategories.RemoveRange(toRemove);

        var existingCategoryIds = currentLinks.Select(item => item.CategoryId).ToHashSet();
        var timestamp = DateTime.UtcNow;

        foreach (var categoryId in validCategoryIds)
        {
            if (existingCategoryIds.Contains(categoryId))
            {
                continue;
            }

            dbContext.GameCategories.Add(new GameCategory
            {
                GameCategoryId = Guid.NewGuid(),
                GameId = gameId,
                CategoryId = categoryId,
                CreatedAt = timestamp
            });
        }
    }

    private async Task<List<Guid>> LoadValidCategoryIdsAsync(IEnumerable<Guid>? categoryIds, CancellationToken cancellationToken)
    {
        var selectedCategoryIds = categoryIds?
            .Where(item => item != Guid.Empty)
            .Distinct()
            .ToList() ?? [];

        if (selectedCategoryIds.Count == 0)
        {
            return [];
        }

        return await dbContext.Categories
            .AsNoTracking()
            .Where(item => selectedCategoryIds.Contains(item.CategoryId))
            .Select(item => item.CategoryId)
            .ToListAsync(cancellationToken);
    }

    private string GetUploadDirectory(string bucket)
    {
        return Path.Combine(webHostEnvironment.ContentRootPath, "App_Data", "admin-workspace", bucket);
    }

    private static IReadOnlyList<string> BuildSteamTags(SteamStoreAppData steamData)
    {
        var tags = new List<string>();

        if (steamData.Genres is not null)
        {
            tags.AddRange(steamData.Genres
                .Select(item => item.Description)
                .Where(item => !string.IsNullOrWhiteSpace(item))!
                .Cast<string>());
        }

        if (!string.IsNullOrWhiteSpace(steamData.ReleaseDate?.Date))
        {
            tags.Add(steamData.ReleaseDate.Date!);
        }

        return tags
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToList();
    }

    private static decimal NormalizeSteamAmount(int? value)
    {
        if (value is null or <= 0)
        {
            return 0;
        }

        return decimal.Round(value.Value / 100M, 0, MidpointRounding.AwayFromZero);
    }

    private static string FormatVnd(decimal amount)
    {
        return string.Format(CultureInfo.GetCultureInfo("vi-VN"), "{0:N0} VND", amount);
    }

    private static string? NormalizeBucket(string bucket)
    {
        return bucket switch
        {
            "images" => "images",
            "files" => "files",
            _ => null
        };
    }

    private static string? CleanOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(character);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
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
}
