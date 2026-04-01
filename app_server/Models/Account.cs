using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace app_server.Models;

[Table("accounts")]
[Index(nameof(VersionId), Name = "IX_accounts_version_id")]
public partial class Account
{
    [Key]
    [Column("account_id")]
    public Guid AccountId { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("is_purchased")]
    public bool IsPurchased { get; set; }

    [Column("version_id")]
    public Guid? VersionId { get; set; }

    [Column("created_at", TypeName = "datetime2(0)")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", TypeName = "datetime2(0)")]
    public DateTime UpdatedAt { get; set; }

    [InverseProperty(nameof(GameFile.Account))]
    public virtual ICollection<GameFile> GameFiles { get; set; } = new List<GameFile>();

    [ForeignKey(nameof(VersionId))]
    [InverseProperty(nameof(GameVersion.Accounts))]
    public virtual GameVersion? Version { get; set; }
}
