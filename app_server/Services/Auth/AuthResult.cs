using app_server.Contracts.Auth;

namespace app_server.Services.Auth;

public class AuthResult
{
    public bool Success { get; set; }

    public string? Error { get; set; }

    public AuthResponse? Response { get; set; }

    public TokenResult? Tokens { get; set; }
}
