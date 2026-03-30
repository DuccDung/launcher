using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace app_server.Controllers.View.Admin;

[Authorize(Roles = "ADMIN")]
public sealed class AdminGameWorkspaceController : Controller
{
    [HttpGet("/admin/games/workspace")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Game Workspace | NestG";
        ViewData["AdminNav"] = "games-workspace";
        ViewData["AdminBodyClass"] = "admin-crud-page admin-game-lab-page";
        ViewData["AdminHeaderTitle"] = "Game workspace theo model thật";
        ViewData["AdminHeaderSubtitle"] = "Game, version, account, file, media và article trên cùng một luồng quản trị.";
        ViewData["AdminSearchPlaceholder"] = "Tìm game theo tên hoặc slug preview...";

        return View("~/Views/Admin/Games/Workspace.cshtml");
    }
}
