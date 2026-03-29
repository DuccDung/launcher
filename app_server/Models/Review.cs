using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace app_server.Models;

[Table("reviews")]
[Index(nameof(GameId), Name = "IX_reviews_game_id")]
[Index(nameof(UserId), Name = "IX_reviews_user_id")]
public partial class Review
{
    [Key]
    [Column("review_id")]
    public Guid ReviewId { get; set; }

    [Column("game_id")]
    public Guid GameId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("rating", TypeName = "decimal(3,2)")]
    public decimal? Rating { get; set; }

    [Column("review_text")]
    public string? ReviewText { get; set; }

    [Column("created_at", TypeName = "datetime2(0)")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", TypeName = "datetime2(0)")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey(nameof(GameId))]
    [InverseProperty(nameof(Game.Reviews))]
    public virtual Game Game { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    [InverseProperty(nameof(User.Reviews))]
    public virtual User User { get; set; } = null!;
}
