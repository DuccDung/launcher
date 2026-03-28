namespace app_server.Contracts.Auth;

public class AuthResponse
{
    public Guid UserId { get; set; }

    public string Email { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string AccessToken { get; set; } = null!;

    public DateTime AccessTokenExpiresAt { get; set; }

    public string RefreshToken { get; set; } = null!;

    public DateTime RefreshTokenExpiresAt { get; set; }
}
