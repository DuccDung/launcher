namespace app_server.Contracts.Auth;

public class OtpChallengeResponse
{
    public string Email { get; set; } = null!;

    public string Purpose { get; set; } = null!;

    public string Message { get; set; } = null!;

    public DateTime ExpiresAtUtc { get; set; }

    public bool EmailDispatched { get; set; }
}
