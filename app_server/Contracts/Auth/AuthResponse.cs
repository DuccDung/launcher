namespace app_server.Contracts.Auth;

public class AuthResponse
{
    public Guid UserId { get; set; }

    public string Email { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public string Status { get; set; } = null!;

    public bool EmailVerified { get; set; }

    public List<string> Roles { get; set; } = [];

    public DateTime AccessTokenExpiresAt { get; set; }

    public DateTime RefreshTokenExpiresAt { get; set; }
}
