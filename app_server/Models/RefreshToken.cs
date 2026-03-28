using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace app_server.Models;

[Table("refresh_tokens")]
[Index("TokenHash", Name = "UQ_refresh_tokens_token_hash", IsUnique = true)]
[Index("UserId", Name = "IX_refresh_tokens_user_id")]
public class RefreshToken
{
    [Key]
    [Column("refresh_token_id")]
    public Guid RefreshTokenId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("token_hash")]
    [StringLength(255)]
    public string TokenHash { get; set; } = null!;

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("revoked_at")]
    public DateTime? RevokedAt { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
