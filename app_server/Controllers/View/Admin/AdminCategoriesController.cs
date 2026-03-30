using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace app_server.Controllers.View.Admin;

[Authorize(Roles = "ADMIN")]
public sealed class AdminCategoriesController : Controller
{
    [HttpGet("/admin/categories")]
    public IActionResult Index()
    {
        ViewData["AdminSubNav"] = "categories-list";
        return View("~/Views/Admin/Categories/Index.cshtml");
    }

    [HttpGet("/admin/categories/create")]
    public IActionResult Create()
    {
        ViewData["AdminSubNav"] = "categories-create";
        return View("~/Views/Admin/Categories/Index.cshtml");
    }
}
