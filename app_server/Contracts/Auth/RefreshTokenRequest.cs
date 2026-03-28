using System.ComponentModel.DataAnnotations;

namespace app_server.Contracts.Auth;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = null!;
}
