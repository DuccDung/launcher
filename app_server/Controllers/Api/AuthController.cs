using app_server.Contracts.Auth;
using app_server.Infrastructure;
using app_server.Services.Auth;
using Microsoft.AspNetCore.Mvc;

namespace app_server.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AuthService authService) : ControllerBase
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
            return CreateErrorResult(result);
        }

        if (result.Tokens is not null)
        {
            AuthCookies.AppendAuthCookies(Response, result.Tokens);
        }

        return result.Challenge is not null
            ? Ok(result.Challenge)
            : Ok(result.Response);
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
            return CreateErrorResult(result);
        }

        if (result.Tokens is not null)
        {
            AuthCookies.AppendAuthCookies(Response, result.Tokens);
        }

        return result.Challenge is not null
            ? Ok(result.Challenge)
            : Ok(result.Response);
    }

    [HttpPost("verify-email-otp")]
    public async Task<IActionResult> VerifyEmailOtp([FromBody] VerifyOtpRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await authService.VerifyEmailOtpAsync(request, cancellationToken);
        if (!result.Success)
        {
            return CreateErrorResult(result);
        }

        AuthCookies.AppendAuthCookies(Response, result.Tokens!);
        return Ok(result.Response);
    }

    [HttpPost("resend-email-otp")]
    public async Task<IActionResult> ResendEmailOtp([FromBody] ResendOtpRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await authService.ResendEmailVerificationOtpAsync(request, cancellationToken);
        if (!result.Success)
        {
            return CreateErrorResult(result);
        }

        return Ok(result.Challenge);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await authService.ForgotPasswordAsync(request, cancellationToken);
        if (!result.Success)
        {
            return CreateErrorResult(result);
        }

        return Ok(result.Challenge);
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await authService.ResetPasswordAsync(request, cancellationToken);
        if (!result.Success)
        {
            return CreateErrorResult(result);
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

        var result = await authService.RefreshAsync(refreshToken, cancellationToken);
        if (!result.Success)
        {
            AuthCookies.ClearAuthCookies(Response);
            return Unauthorized(new { message = result.Error });
        }

        AuthCookies.AppendAuthCookies(Response, result.Tokens!);
        return Ok(result.Response);
    }

    private IActionResult CreateErrorResult(AuthResult result)
    {
        var payload = new
        {
            code = result.ErrorCode,
            message = result.Error
        };

        return result.ErrorCode switch
        {
            AuthErrorCodes.UserNotFound => NotFound(payload),
            AuthErrorCodes.InvalidCredentials => Unauthorized(payload),
            AuthErrorCodes.EmailAlreadyExists => Conflict(payload),
            AuthErrorCodes.AccountBlocked => StatusCode(StatusCodes.Status403Forbidden, payload),
            AuthErrorCodes.EmailVerificationRequired => StatusCode(StatusCodes.Status403Forbidden, payload),
            _ => BadRequest(payload)
        };
    }
}
