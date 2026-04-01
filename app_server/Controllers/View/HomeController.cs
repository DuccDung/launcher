using app_server.Models;
using app_server.Services.Storefront;
using app_server.ViewModels.Storefront;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace app_server.Controllers.View;

public sealed class HomeController(LauncherDbContext dbContext, ILogger<HomeController> logger) : Controller
{
    public async Task<IActionResult> Index(string? q, CancellationToken cancellationToken)
    {
        var keyword = string.IsNullOrWhiteSpace(q) ? null : q.Trim();

        var games = await dbContext.Games
            .AsNoTracking()
            .Include(item => item.GameCategories)
                .ThenInclude(item => item.Category)
            .Include(item => item.MediaItems)
            .Where(item => item.SteamAppId != null &&
                           (keyword == null ||
                            item.Name.Contains(keyword) ||
                            item.GameCategories.Any(category => category.Category != null && category.Category.Name.Contains(keyword))))
            .OrderByDescending(item => item.UpdatedAt)
            .Take(24)
            .ToListAsync(cancellationToken);

        var model = new StorefrontHomeViewModel
        {
            Games = games.Select(StorefrontViewModelFactory.ToProductCard).ToArray()
        };

        ViewData["Title"] = "Deluxe Gaming";
        ViewData["StoreActive"] = "home";
        ViewData["SearchQuery"] = keyword;
        return View(model);
    }

    public IActionResult Privacy()
    {
        logger.LogInformation("Privacy page was requested.");
        return View();
    }
}
