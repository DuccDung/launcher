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
            return CreateErrorResult(result);
        }

        return result.Challenge is not null
            ? Ok(result.Challenge)
            : Ok(CreateClientAuthResponse(result));
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

        return result.Challenge is not null
            ? Ok(result.Challenge)
            : Ok(CreateClientAuthResponse(result));
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

        return Ok(CreateClientAuthResponse(result));
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
            DisplayName = result.Response.DisplayName,
            Status = result.Response.Status,
            EmailVerified = result.Response.EmailVerified,
            Roles = result.Response.Roles,
            AccessToken = result.Tokens!.AccessToken,
            AccessTokenExpiresAt = result.Tokens.AccessTokenExpiresAt,
            RefreshToken = result.Tokens.RefreshToken,
            RefreshTokenExpiresAt = result.Tokens.RefreshTokenExpiresAt
        };
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
