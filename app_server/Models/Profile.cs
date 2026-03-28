using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace app_server.Models;

[Table("profiles")]
[Index("IsVip", Name = "IX_profiles_is_vip")]
[Index("UserId", Name = "UQ_profiles_user_id", IsUnique = true)]
public partial class Profile
{
    [Key]
    [Column("profile_id")]
    public Guid ProfileId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("display_name")]
    [StringLength(100)]
    public string? DisplayName { get; set; }

    [Column("avatar_url")]
    [StringLength(500)]
    public string? AvatarUrl { get; set; }

    [Column("is_vip")]
    public bool IsVip { get; set; }

    [Column("vip_purchased_at")]
    public DateTime? VipPurchasedAt { get; set; }

    [Column("vip_expires_at")]
    public DateTime? VipExpiresAt { get; set; }

    [Column("bio")]
    [StringLength(1000)]
    public string? Bio { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Profile")]
    public virtual User User { get; set; } = null!;
}
