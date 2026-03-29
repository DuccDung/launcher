using app_server.Contracts.Auth;
using app_server.Infrastructure;
using app_server.Services.Auth;
using Microsoft.AspNetCore.Mvc;

namespace app_server.Controllers.Api.Admin;

[ApiController]
[Route("api/admin/auth")]
public class AdminAuthController(AuthService authService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await authService.AdminLoginAsync(request, cancellationToken);
        if (!result.Success)
        {
            AuthCookies.ClearAuthCookies(Response);
            return Unauthorized(new { message = result.Error });
        }

        AuthCookies.AppendAuthCookies(Response, result.Tokens!);
        return Ok(result.Response);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(AuthCookies.RefreshTokenCookieName, out var refreshToken) ||
            string.IsNullOrWhiteSpace(refreshToken))
        {
            AuthCookies.ClearAuthCookies(Response);
            return Unauthorized(new { message = "Refresh token is missing." });
        }

        var result = await authService.AdminRefreshAsync(refreshToken, cancellationToken);
        if (!result.Success)
        {
            AuthCookies.ClearAuthCookies(Response);
            return Unauthorized(new { message = result.Error });
        }

        AuthCookies.AppendAuthCookies(Response, result.Tokens!);
        return Ok(result.Response);
    }
}
