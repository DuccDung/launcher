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

    [Column("steam_price", TypeName = "decimal(18,2)")]
    public decimal? SteamPrice { get; set; }

    [Column("photo_url", TypeName = "nvarchar(500)")]
    [StringLength(500)]
    public string? PhotoUrl { get; set; }

    [Column("is_trending")]
    public bool IsTrending { get; set; }

    [Column("isremove")]
    public bool IsRemove { get; set; }

    [Column("steam_app_id")]
    public int? SteamAppId { get; set; }

    [Column("created_at", TypeName = "datetime2(0)")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", TypeName = "datetime2(0)")]
    public DateTime UpdatedAt { get; set; }

    [InverseProperty(nameof(GameCategory.Game))]
    public virtual ICollection<GameCategory> GameCategories { get; set; } = new List<GameCategory>();

    [InverseProperty(nameof(GameVersion.Game))]
    public virtual ICollection<GameVersion> GameVersions { get; set; } = new List<GameVersion>();

    [InverseProperty(nameof(CartItem.Game))]
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    [InverseProperty(nameof(Media.Game))]
    public virtual ICollection<Media> MediaItems { get; set; } = new List<Media>();

    [InverseProperty(nameof(Review.Game))]
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
