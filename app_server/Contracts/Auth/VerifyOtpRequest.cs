using System.ComponentModel.DataAnnotations;

namespace app_server.Contracts.Auth;

public class VerifyOtpRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = null!;

    [Required]
    [StringLength(12, MinimumLength = 4)]
    public string Otp { get; set; } = null!;
}
