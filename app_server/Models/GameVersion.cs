using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace app_server.Models;

[Table("game_versions")]
[Index(nameof(GameId), Name = "IX_game_versions_game_id")]
public partial class GameVersion
{
    [Key]
    [Column("version_id")]
    public Guid VersionId { get; set; }

    [Column("game_id")]
    public Guid GameId { get; set; }

    [Column("version_name")]
    [StringLength(255)]
    public string? VersionName { get; set; }

    [Column("created_at", TypeName = "datetime2(0)")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", TypeName = "datetime2(0)")]
    public DateTime UpdatedAt { get; set; }

    [Column("is_removed")]
    public bool IsRemoved { get; set; }

    [ForeignKey(nameof(GameId))]
    [InverseProperty(nameof(Game.GameVersions))]
    public virtual Game Game { get; set; } = null!;

    [InverseProperty(nameof(Account.Version))]
    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
}
