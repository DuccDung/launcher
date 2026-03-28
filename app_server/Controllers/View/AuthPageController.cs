using Microsoft.AspNetCore.Mvc;

namespace app_server.Controllers.View;

public class AuthPageController : Controller
{
    [HttpGet("/auth")]
    public IActionResult Index()
    {
        return View();
    }
}
