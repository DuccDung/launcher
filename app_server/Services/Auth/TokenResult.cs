namespace app_server.Services.Auth;

public class TokenResult
{
    public string AccessToken { get; set; } = null!;

    public DateTime AccessTokenExpiresAt { get; set; }

    public string RefreshToken { get; set; } = null!;

    public string RefreshTokenHash { get; set; } = null!;

    public DateTime RefreshTokenExpiresAt { get; set; }
}
