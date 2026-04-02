using app_server.Models;
using app_server.Services.Storefront;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace app_server.Controllers.View;

public sealed class ProductController(
    LauncherDbContext dbContext,
    ISteamStoreService steamStoreService) : Controller
{
    [HttpGet("/products/{gameId:guid}/{slug?}")]
    public async Task<IActionResult> Detail(Guid gameId, string? slug, CancellationToken cancellationToken)
    {
        var game = await dbContext.Games
            .AsNoTracking()
            .Include(item => item.GameCategories)
                .ThenInclude(item => item.Category)
            .Include(item => item.GameVersions)
            .Include(item => item.MediaItems)
            .Include(item => item.Reviews)
                .ThenInclude(item => item.User)
                    .ThenInclude(item => item.Profile)
            .SingleOrDefaultAsync(item => item.GameId == gameId && !item.IsRemove, cancellationToken);

        if (game is null || game.SteamAppId is null or <= 0)
        {
            return NotFound();
        }

        var steamData = await steamStoreService.GetAppDetailsAsync(game.SteamAppId.Value, cancellationToken);
        if (steamData is null)
        {
            return StatusCode(StatusCodes.Status502BadGateway, "Không thể tải dữ liệu game từ Steam lúc này.");
        }

        var currentCategoryIds = game.GameCategories.Select(item => item.CategoryId).ToHashSet();
        var relatedCandidates = await dbContext.Games
            .AsNoTracking()
            .Include(item => item.GameCategories)
                .ThenInclude(item => item.Category)
            .Include(item => item.MediaItems)
            .Include(item => item.GameVersions)
            .Where(item => item.GameId != gameId &&
                           !item.IsRemove &&
                           item.SteamAppId != null)
            .OrderByDescending(item => item.UpdatedAt)
            .Take(24)
            .ToListAsync(cancellationToken);

        var relatedProducts = relatedCandidates
            .OrderByDescending(item => item.GameCategories.Count(category => currentCategoryIds.Contains(category.CategoryId)))
            .ThenByDescending(item => item.UpdatedAt)
            .Take(8)
            .Select(StorefrontViewModelFactory.ToProductCard)
            .ToArray();

        var model = StorefrontViewModelFactory.ToProductDetail(game, steamData, relatedProducts);
        if (!string.IsNullOrWhiteSpace(slug) &&
            !string.Equals(slug, model.Slug, StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToActionPermanent(nameof(Detail), new { gameId, slug = model.Slug });
        }

        ViewData["Title"] = model.PageTitle;
        return View(model);
    }
}
