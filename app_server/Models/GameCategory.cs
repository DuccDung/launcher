using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace app_server.Models;

[Table("game_categories")]
[Index(nameof(GameId), Name = "IX_game_categories_game_id")]
[Index(nameof(CategoryId), Name = "IX_game_categories_category_id")]
[Index(nameof(GameId), nameof(CategoryId), Name = "UQ_game_categories_game_id_category_id", IsUnique = true)]
public partial class GameCategory
{
    [Key]
    [Column("game_category_id")]
    public Guid GameCategoryId { get; set; }

    [Column("game_id")]
    public Guid GameId { get; set; }

    [Column("category_id")]
    public Guid CategoryId { get; set; }

    [Column("created_at", TypeName = "datetime2(0)")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(GameId))]
    [InverseProperty(nameof(Game.GameCategories))]
    public virtual Game Game { get; set; } = null!;

    [ForeignKey(nameof(CategoryId))]
    [InverseProperty(nameof(Category.GameCategories))]
    public virtual Category Category { get; set; } = null!;
}
