using Microsoft.AspNetCore.Mvc;

namespace app_server.Controllers.View.Admin;

public class AdminAuthController : Controller
{
    [HttpGet("/admin/login")]
    public IActionResult Login()
    {
        return View("~/Views/Admin/Auth/Login.cshtml");
    }
}
