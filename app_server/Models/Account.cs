using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace app_server.Models;

[Table("accounts")]
public partial class Account
{
    [Key]
    [Column("account_id")]
    public Guid AccountId { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("is_purchased")]
    public bool IsPurchased { get; set; }

    [Column("created_at", TypeName = "datetime2(0)")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", TypeName = "datetime2(0)")]
    public DateTime UpdatedAt { get; set; }

    [InverseProperty(nameof(GameVersion.Account))]
    public virtual ICollection<GameVersion> GameVersions { get; set; } = new List<GameVersion>();
}
