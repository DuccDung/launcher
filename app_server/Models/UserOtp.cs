using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace app_server.Models;

[Table("user_otps")]
[Index(nameof(UserId), Name = "IX_user_otps_user_id")]
[Index(nameof(Email), nameof(Purpose), Name = "IX_user_otps_email_purpose")]
public class UserOtp
{
    [Key]
    [Column("user_otp_id")]
    public Guid UserOtpId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("email")]
    [StringLength(255)]
    public string Email { get; set; } = null!;

    [Column("purpose")]
    [StringLength(50)]
    public string Purpose { get; set; } = null!;

    [Column("otp_code")]
    [StringLength(12)]
    public string OtpCode { get; set; } = null!;

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("is_used")]
    public bool IsUsed { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("used_at")]
    public DateTime? UsedAt { get; set; }

    [ForeignKey(nameof(UserId))]
    [InverseProperty(nameof(User.UserOtps))]
    public User User { get; set; } = null!;
}
