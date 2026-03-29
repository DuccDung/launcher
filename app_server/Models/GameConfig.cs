using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace app_server.Models;

[Table("game_configs")]
[Index(nameof(GameId), Name = "IX_game_configs_game_id")]
public partial class GameConfig
{
    [Key]
    [Column("config_id")]
    public Guid ConfigId { get; set; }

    [Column("game_id")]
    public Guid GameId { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("config_type")]
    [StringLength(100)]
    public string? ConfigType { get; set; }

    [Column("created_at", TypeName = "datetime2(0)")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", TypeName = "datetime2(0)")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey(nameof(GameId))]
    [InverseProperty(nameof(Game.GameConfigs))]
    public virtual Game Game { get; set; } = null!;
}
