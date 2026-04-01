using System.ComponentModel.DataAnnotations;

namespace app_server.Contracts.Auth;

public class ResendOtpRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = null!;
}
