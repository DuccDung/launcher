using app_server.Contracts.Auth;
using app_server.Services.Auth;
using Microsoft.AspNetCore.Mvc;

namespace app_server.Controllers.Api;

[ApiController]
[Route("api/desktop-auth")]
public class DesktopAuthController(AuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await authService.RegisterAsync(request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(CreateClientAuthResponse(result));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await authService.LoginAsync(request, cancellationToken);
        if (!result.Success)
        {
            return Unauthorized(new { message = result.Error });
        }

        return Ok(CreateClientAuthResponse(result));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await authService.RefreshAsync(request.RefreshToken, cancellationToken);
        if (!result.Success)
        {
            return Unauthorized(new { message = result.Error });
        }

        return Ok(CreateClientAuthResponse(result));
    }

    private static ClientAuthResponse CreateClientAuthResponse(AuthResult result)
    {
        return new ClientAuthResponse
        {
            UserId = result.Response!.UserId,
            Email = result.Response.Email,
            Status = result.Response.Status,
            Roles = result.Response.Roles,
            AccessToken = result.Tokens!.AccessToken,
            AccessTokenExpiresAt = result.Tokens.AccessTokenExpiresAt,
            RefreshToken = result.Tokens.RefreshToken,
            RefreshTokenExpiresAt = result.Tokens.RefreshTokenExpiresAt
        };
    }
}
