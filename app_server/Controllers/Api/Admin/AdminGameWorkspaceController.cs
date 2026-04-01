using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using app_server.Contracts.Admin.GameWorkspace;
using app_server.Models;
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
    IWebHostEnvironment webHostEnvironment) : ControllerBase
{
    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".webp", ".gif", ".bmp"];

    [HttpGet("bootstrap")]
    public async Task<ActionResult<AdminWorkspaceBootstrapResponse>> GetBootstrap(CancellationToken cancellationToken)
    {
        return Ok(await BuildBootstrapResponseAsync(cancellationToken));
    }

    [HttpGet("games/{gameId:guid}")]
    public async Task<ActionResult<AdminWorkspaceDetailsResponse>> GetGameWorkspace(Guid gameId, CancellationToken cancellationToken)
    {
        var response = await BuildGameWorkspaceResponseAsync(gameId, cancellationToken);
        return response is null
            ? NotFound(new { message = "Không tìm thấy game workspace." })
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
            ModelState.AddModelError(nameof(request.Name), "Tên game là bắt buộc.");
            return ValidationProblem(ModelState);
        }

        var timestamp = DateTime.UtcNow;
        var game = new Game
        {
            Name = request.Name.Trim(),
            Rating = request.Rating,
            OldPrice = request.OldPrice,
            NewPrice = request.NewPrice,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };

        dbContext.Games.Add(game);
        await dbContext.SaveChangesAsync(cancellationToken);
        await SyncGameCategoriesAsync(game.GameId, request.CategoryIds, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "Đã tạo game mới.",
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
            return NotFound(new { message = "Không tìm thấy game để cập nhật." });
        }

        game.Name = request.Name.Trim();
        game.Rating = request.Rating;
        game.OldPrice = request.OldPrice;
        game.NewPrice = request.NewPrice;
        game.UpdatedAt = DateTime.UtcNow;

        await SyncGameCategoriesAsync(game.GameId, request.CategoryIds, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Đã cập nhật game." });
    }

    [HttpDelete("games/{gameId:guid}")]
    public async Task<IActionResult> DeleteGame(Guid gameId, CancellationToken cancellationToken)
    {
        var game = await dbContext.Games.SingleOrDefaultAsync(item => item.GameId == gameId, cancellationToken);
        if (game is null)
        {
            return NotFound(new { message = "Không tìm thấy game để xóa." });
        }

        var hasReviews = await dbContext.Reviews.AnyAsync(item => item.GameId == gameId, cancellationToken);
        if (hasReviews)
        {
            return Conflict(new { message = "Game đang có review, hãy xử lý review trước khi xóa." });
        }

        var categories = await dbContext.GameCategories.Where(item => item.GameId == gameId).ToListAsync(cancellationToken);
        var versions = await dbContext.GameVersions.Where(item => item.GameId == gameId).ToListAsync(cancellationToken);
        var versionIds = versions.Select(item => item.VersionId).ToList();
        var linkedAccounts = await dbContext.Accounts
            .Where(item => item.VersionId.HasValue && versionIds.Contains(item.VersionId.Value))
            .ToListAsync(cancellationToken);
        var mediaItems = await dbContext.Media.Where(item => item.GameId == gameId).ToListAsync(cancellationToken);
        var articles = await dbContext.Articles.Where(item => item.GameId == gameId).ToListAsync(cancellationToken);
        var configs = await dbContext.GameConfigs.Where(item => item.GameId == gameId).ToListAsync(cancellationToken);

        foreach (var account in linkedAccounts)
        {
            account.VersionId = null;
            account.UpdatedAt = DateTime.UtcNow;
        }

        dbContext.GameCategories.RemoveRange(categories);
        dbContext.GameVersions.RemoveRange(versions);
        dbContext.Media.RemoveRange(mediaItems);
        dbContext.Articles.RemoveRange(articles);
        dbContext.GameConfigs.RemoveRange(configs);
        dbContext.Games.Remove(game);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Đã xóa game và các quan hệ workspace liên quan." });
    }

    [HttpPost("games/{gameId:guid}/versions")]
    public async Task<IActionResult> CreateVersion(Guid gameId, [FromBody] AdminGameVersionUpsertRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.VersionName))
        {
            return BadRequest(new { message = "Version name là bắt buộc." });
        }

        if (!await dbContext.Games.AnyAsync(item => item.GameId == gameId, cancellationToken))
        {
            return NotFound(new { message = "Không tìm thấy game để tạo version." });
        }

        var timestamp = DateTime.UtcNow;
        var version = new GameVersion
        {
            GameId = gameId,
            VersionName = CleanOptionalText(request.VersionName),
            IsRemoved = request.IsRemoved,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };

        dbContext.GameVersions.Add(version);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Đã tạo version mới.", versionId = version.VersionId });
    }

    [HttpPut("versions/{versionId:guid}")]
    public async Task<IActionResult> UpdateVersion(Guid versionId, [FromBody] AdminGameVersionUpsertRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.VersionName))
        {
            return BadRequest(new { message = "Version name là bắt buộc." });
        }

        var version = await dbContext.GameVersions.SingleOrDefaultAsync(item => item.VersionId == versionId, cancellationToken);
        if (version is null)
        {
            return NotFound(new { message = "Không tìm thấy version để cập nhật." });
        }

        version.VersionName = CleanOptionalText(request.VersionName);
        version.IsRemoved = request.IsRemoved;
        version.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Đã cập nhật version." });
    }

    [HttpDelete("versions/{versionId:guid}")]
    public async Task<IActionResult> DeleteVersion(Guid versionId, CancellationToken cancellationToken)
    {
        var version = await dbContext.GameVersions.SingleOrDefaultAsync(item => item.VersionId == versionId, cancellationToken);
        if (version is null)
        {
            return NotFound(new { message = "Không tìm thấy version để xóa." });
        }

        var linkedAccounts = await dbContext.Accounts.Where(item => item.VersionId == versionId).ToListAsync(cancellationToken);
        foreach (var account in linkedAccounts)
        {
            account.VersionId = null;
            account.UpdatedAt = DateTime.UtcNow;
        }

        dbContext.GameVersions.Remove(version);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Đã xóa version và gỡ account liên quan." });
    }

    [HttpPost("accounts")]
    public async Task<IActionResult> CreateAccount([FromBody] AdminAccountUpsertRequest request, CancellationToken cancellationToken)
    {
        if (!request.VersionId.HasValue)
        {
            return BadRequest(new { message = "Hãy chọn version trước khi tạo account." });
        }

        var version = await dbContext.GameVersions.SingleOrDefaultAsync(item => item.VersionId == request.VersionId.Value, cancellationToken);
        if (version is null)
        {
            return BadRequest(new { message = "Version được chọn không tồn tại." });
        }

        var timestamp = DateTime.UtcNow;
        var account = new Account
        {
            VersionId = version.VersionId,
            IsActive = request.IsActive,
            IsPurchased = false,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };

        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Đã tạo account mới.", accountId = account.AccountId });
    }

    [HttpPut("accounts/{accountId:guid}")]
    public async Task<IActionResult> UpdateAccount(Guid accountId, [FromBody] AdminAccountUpsertRequest request, CancellationToken cancellationToken)
    {
        var account = await dbContext.Accounts.SingleOrDefaultAsync(item => item.AccountId == accountId, cancellationToken);
        if (account is null)
        {
            return NotFound(new { message = "Không tìm thấy account để cập nhật." });
        }

        if (request.VersionId.HasValue &&
            !await dbContext.GameVersions.AnyAsync(item => item.VersionId == request.VersionId.Value, cancellationToken))
        {
            return BadRequest(new { message = "Version được chọn không tồn tại." });
        }

        account.VersionId = request.VersionId;
        account.IsActive = request.IsActive;
        account.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Đã cập nhật account." });
    }

    [HttpDelete("accounts/{accountId:guid}")]
    public async Task<IActionResult> DeleteAccount(Guid accountId, CancellationToken cancellationToken)
    {
        var account = await dbContext.Accounts.SingleOrDefaultAsync(item => item.AccountId == accountId, cancellationToken);
        if (account is null)
        {
            return NotFound(new { message = "Không tìm thấy account để xóa." });
        }

        var linkedFiles = await dbContext.GameFiles.Where(item => item.AccountId == accountId).ToListAsync(cancellationToken);
        dbContext.GameFiles.RemoveRange(linkedFiles);
        dbContext.Accounts.Remove(account);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Đã xóa account và file liên quan." });
    }

    [HttpPost("files")]
    public async Task<IActionResult> CreateFile([FromBody] AdminGameFileUpsertRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!await dbContext.Accounts.AnyAsync(item => item.AccountId == request.AccountId, cancellationToken))
        {
            return BadRequest(new { message = "Account được chọn không tồn tại." });
        }

        var timestamp = DateTime.UtcNow;
        var fileRecord = new GameFile
        {
            AccountId = request.AccountId,
            FileType = CleanOptionalText(request.FileType),
            IsActive = request.IsActive,
            FileUrl01 = CleanOptionalText(request.FileUrl01),
            FileUrl02 = CleanOptionalText(request.FileUrl02),
            FileUrl03 = CleanOptionalText(request.FileUrl03),
            FileUrl04 = CleanOptionalText(request.FileUrl04),
            FileUrl05 = CleanOptionalText(request.FileUrl05),
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };

        dbContext.GameFiles.Add(fileRecord);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Đã tạo file package mới.", fileId = fileRecord.FileId });
    }

    [HttpPut("files/{fileId:guid}")]
    public async Task<IActionResult> UpdateFile(Guid fileId, [FromBody] AdminGameFileUpsertRequest request, CancellationToken cancellationToken)
    {
        var fileRecord = await dbContext.GameFiles.SingleOrDefaultAsync(item => item.FileId == fileId, cancellationToken);
        if (fileRecord is null)
        {
            return NotFound(new { message = "Không tìm thấy file package để cập nhật." });
        }

        if (!await dbContext.Accounts.AnyAsync(item => item.AccountId == request.AccountId, cancellationToken))
        {
            return BadRequest(new { message = "Account được chọn không tồn tại." });
        }

        fileRecord.AccountId = request.AccountId;
        fileRecord.FileType = CleanOptionalText(request.FileType);
        fileRecord.IsActive = request.IsActive;
        fileRecord.FileUrl01 = CleanOptionalText(request.FileUrl01);
        fileRecord.FileUrl02 = CleanOptionalText(request.FileUrl02);
        fileRecord.FileUrl03 = CleanOptionalText(request.FileUrl03);
        fileRecord.FileUrl04 = CleanOptionalText(request.FileUrl04);
        fileRecord.FileUrl05 = CleanOptionalText(request.FileUrl05);
        fileRecord.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Đã cập nhật file package." });
    }

    [HttpDelete("files/{fileId:guid}")]
    public async Task<IActionResult> DeleteFile(Guid fileId, CancellationToken cancellationToken)
    {
        var fileRecord = await dbContext.GameFiles.SingleOrDefaultAsync(item => item.FileId == fileId, cancellationToken);
        if (fileRecord is null)
        {
            return NotFound(new { message = "Không tìm thấy file package để xóa." });
        }

        dbContext.GameFiles.Remove(fileRecord);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Đã xóa file package." });
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
            return NotFound(new { message = "Không tìm thấy game để thêm media." });
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
        return Ok(new { message = "Đã tạo media mới.", mediaId = media.MediaId });
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
            return NotFound(new { message = "Không tìm thấy media để cập nhật." });
        }

        media.MediaType = CleanOptionalText(request.MediaType);
        media.Url = request.Url.Trim();
        media.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Đã cập nhật media." });
    }

    [HttpDelete("media/{mediaId:guid}")]
    public async Task<IActionResult> DeleteMedia(Guid mediaId, CancellationToken cancellationToken)
    {
        var media = await dbContext.Media.SingleOrDefaultAsync(item => item.MediaId == mediaId, cancellationToken);
        if (media is null)
        {
            return NotFound(new { message = "Không tìm thấy media để xóa." });
        }

        dbContext.Media.Remove(media);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Đã xóa media." });
    }

    [HttpPut("games/{gameId:guid}/article")]
    public async Task<IActionResult> UpsertArticle(Guid gameId, [FromBody] AdminArticleUpsertRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!await dbContext.Games.AnyAsync(item => item.GameId == gameId, cancellationToken))
        {
            return NotFound(new { message = "Không tìm thấy game để lưu article." });
        }

        if (!TryNormalizeJson(request.ContentJson, out var contentJson))
        {
            return BadRequest(new { message = "ContentJson không phải JSON hợp lệ." });
        }

        var article = await dbContext.Articles.SingleOrDefaultAsync(item => item.GameId == gameId, cancellationToken);
        if (article is null)
        {
            article = new Article
            {
                GameId = gameId,
                Summary = CleanOptionalText(request.Summary),
                ContentJson = contentJson,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.Articles.Add(article);
        }
        else
        {
            article.Summary = CleanOptionalText(request.Summary);
            article.ContentJson = contentJson;
            article.UpdatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Đã lưu article.", articleId = article.ArticleId });
    }

    [HttpDelete("articles/{articleId:guid}")]
    public async Task<IActionResult> DeleteArticle(Guid articleId, CancellationToken cancellationToken)
    {
        var article = await dbContext.Articles.SingleOrDefaultAsync(item => item.ArticleId == articleId, cancellationToken);
        if (article is null)
        {
            return NotFound(new { message = "Không tìm thấy article để xóa." });
        }

        dbContext.Articles.Remove(article);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Đã xóa article." });
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
            return BadRequest(new { message = "Không có file nào được gửi lên." });
        }

        var extension = Path.GetExtension(file.FileName);
        if (mustBeImage && !ImageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "File ảnh chỉ hỗ trợ png, jpg, jpeg, webp, gif, bmp." });
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
            .OrderByDescending(item => item.UpdatedAt)
            .ThenBy(item => item.Name)
            .Select(item => new
            {
                item.GameId,
                item.Name,
                item.Rating,
                item.OldPrice,
                item.NewPrice,
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
            .Where(item => gameIds.Contains(item.GameId))
            .GroupBy(item => item.GameId)
            .Select(group => new { GameId = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var mediaCounts = await dbContext.Media
            .AsNoTracking()
            .Where(item => item.GameId.HasValue && gameIds.Contains(item.GameId.Value))
            .GroupBy(item => item.GameId!.Value)
            .Select(group => new { GameId = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var articleGameIds = await dbContext.Articles
            .AsNoTracking()
            .Where(item => gameIds.Contains(item.GameId))
            .Select(item => item.GameId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var versionCountMap = versionCounts.ToDictionary(item => item.GameId, item => item.Count);
        var mediaCountMap = mediaCounts.ToDictionary(item => item.GameId, item => item.Count);
        var articleGameIdSet = articleGameIds.ToHashSet();
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
                    item.Rating,
                    item.OldPrice,
                    item.NewPrice,
                    categoriesForGame?.CategoryIds ?? Array.Empty<Guid>(),
                    categoriesForGame?.CategoryNames ?? Array.Empty<string>(),
                    versionCountMap.GetValueOrDefault(item.GameId, 0),
                    mediaCountMap.GetValueOrDefault(item.GameId, 0),
                    articleGameIdSet.Contains(item.GameId),
                    item.UpdatedAt);
            })
            .ToList();

        var accounts = await BuildAccountResponsesAsync(cancellationToken);

        var stats = new AdminWorkspaceStatsResponse(
            TotalGames: games.Count,
            TotalVersions: await dbContext.GameVersions.AsNoTracking().CountAsync(cancellationToken),
            TotalAccounts: accounts.Count,
            TotalFiles: await dbContext.GameFiles.AsNoTracking().CountAsync(cancellationToken),
            TotalMedia: await dbContext.Media.AsNoTracking().CountAsync(cancellationToken),
            TotalArticles: await dbContext.Articles.AsNoTracking().CountAsync(cancellationToken));

        return new AdminWorkspaceBootstrapResponse(stats, categories, gameItems, accounts);
    }

    private async Task<List<AdminWorkspaceAccountResponse>> BuildAccountResponsesAsync(CancellationToken cancellationToken)
    {
        var accounts = await dbContext.Accounts
            .AsNoTracking()
            .OrderByDescending(item => item.UpdatedAt)
            .Select(item => new
            {
                item.AccountId,
                item.VersionId,
                item.IsActive,
                item.IsPurchased,
                item.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        var accountIds = accounts.Select(item => item.AccountId).ToList();

        var fileCounts = await dbContext.GameFiles
            .AsNoTracking()
            .Where(item => accountIds.Contains(item.AccountId))
            .GroupBy(item => item.AccountId)
            .Select(group => new { AccountId = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var fileCountMap = fileCounts.ToDictionary(item => item.AccountId, item => item.Count);

        return accounts
            .Select(item => new AdminWorkspaceAccountResponse(
                item.AccountId,
                item.VersionId,
                item.IsActive,
                item.IsPurchased,
                fileCountMap.GetValueOrDefault(item.AccountId, 0),
                item.UpdatedAt))
            .ToList();
    }

    private async Task<AdminWorkspaceDetailsResponse?> BuildGameWorkspaceResponseAsync(Guid gameId, CancellationToken cancellationToken)
    {
        var game = await dbContext.Games
            .AsNoTracking()
            .Where(item => item.GameId == gameId)
            .Select(item => new
            {
                item.GameId,
                item.Name,
                item.Rating,
                item.OldPrice,
                item.NewPrice,
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
            .OrderByDescending(item => item.UpdatedAt)
            .Select(item => new
            {
                item.VersionId,
                item.GameId,
                item.VersionName,
                item.IsRemoved,
                item.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        var versionIds = versions
            .Select(item => item.VersionId)
            .ToList();

        var linkedAccounts = await dbContext.Accounts
            .AsNoTracking()
            .Where(item => item.VersionId.HasValue && versionIds.Contains(item.VersionId.Value))
            .Select(item => new { item.AccountId, item.VersionId })
            .ToListAsync(cancellationToken);

        var linkedAccountIds = linkedAccounts
            .Select(item => item.AccountId)
            .Distinct()
            .ToList();

        var linkedAccountCountMap = linkedAccounts
            .GroupBy(item => item.VersionId!.Value)
            .ToDictionary(group => group.Key, group => group.Count());

        var versionResponses = versions
            .Select(item => new AdminWorkspaceVersionResponse(
                item.VersionId,
                item.GameId,
                item.VersionName,
                linkedAccountCountMap.GetValueOrDefault(item.VersionId, 0),
                item.IsRemoved,
                item.UpdatedAt))
            .ToList();

        var files = await dbContext.GameFiles
            .AsNoTracking()
            .Where(item => linkedAccountIds.Contains(item.AccountId))
            .OrderByDescending(item => item.UpdatedAt)
            .Select(item => new AdminWorkspaceFileResponse(
                item.FileId,
                item.AccountId,
                item.FileType,
                item.IsActive,
                item.FileUrl01,
                item.FileUrl02,
                item.FileUrl03,
                item.FileUrl04,
                item.FileUrl05,
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

        var article = await dbContext.Articles
            .AsNoTracking()
            .Where(item => item.GameId == gameId)
            .Select(item => new AdminWorkspaceArticleResponse(
                item.ArticleId,
                item.GameId,
                item.Summary,
                item.ContentJson ?? "{}",
                item.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);

        return new AdminWorkspaceDetailsResponse(
            new AdminWorkspaceGameResponse(
                game.GameId,
                game.Name,
                Slugify(game.Name),
                game.Rating,
                game.OldPrice,
                game.NewPrice,
                categoryIds,
                game.UpdatedAt),
            versionResponses,
            files,
            mediaItems,
            article);
    }

    private async Task SyncGameCategoriesAsync(Guid gameId, IEnumerable<Guid> categoryIds, CancellationToken cancellationToken)
    {
        var selectedCategoryIds = categoryIds
            .Where(item => item != Guid.Empty)
            .Distinct()
            .ToList();

        var validCategoryIds = await dbContext.Categories
            .AsNoTracking()
            .Where(item => selectedCategoryIds.Contains(item.CategoryId))
            .Select(item => item.CategoryId)
            .ToListAsync(cancellationToken);

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
    private string GetUploadDirectory(string bucket)
    {
        return Path.Combine(webHostEnvironment.ContentRootPath, "App_Data", "admin-workspace", bucket);
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

    private static string CleanOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static bool TryNormalizeJson(string? json, out string normalizedJson)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            normalizedJson = string.Empty;
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            normalizedJson = JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            return true;
        }
        catch
        {
            normalizedJson = string.Empty;
            return false;
        }
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
