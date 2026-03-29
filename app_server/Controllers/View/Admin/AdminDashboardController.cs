using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace app_server.Controllers.View.Admin;

[Authorize(Roles = "ADMIN")]
public class AdminDashboardController : Controller
{
    [HttpGet("/admin/dashboard")]
    public IActionResult Index()
    {
        return View("~/Views/Admin/Dashboard/Index.cshtml");
    }
}
