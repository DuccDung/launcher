using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace app_server.Models;

[Table("games")]
public partial class Game
{
    [Key]
    [Column("game_id")]
    public Guid GameId { get; set; }

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("rating", TypeName = "decimal(3,2)")]
    public decimal? Rating { get; set; }

    [Column("old_price", TypeName = "decimal(18,2)")]
    public decimal? OldPrice { get; set; }

    [Column("new_price", TypeName = "decimal(18,2)")]
    public decimal? NewPrice { get; set; }

    [Column("created_at", TypeName = "datetime2(0)")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", TypeName = "datetime2(0)")]
    public DateTime UpdatedAt { get; set; }

    [InverseProperty(nameof(Article.Game))]
    public virtual ICollection<Article> Articles { get; set; } = new List<Article>();

    [InverseProperty(nameof(GameCategory.Game))]
    public virtual ICollection<GameCategory> GameCategories { get; set; } = new List<GameCategory>();

    [InverseProperty(nameof(GameConfig.Game))]
    public virtual ICollection<GameConfig> GameConfigs { get; set; } = new List<GameConfig>();

    [InverseProperty(nameof(GameVersion.Game))]
    public virtual ICollection<GameVersion> GameVersions { get; set; } = new List<GameVersion>();

    [InverseProperty(nameof(Media.Game))]
    public virtual ICollection<Media> MediaItems { get; set; } = new List<Media>();

    [InverseProperty(nameof(Review.Game))]
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
