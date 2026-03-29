using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace app_server.Models;

[Table("articles")]
[Index(nameof(GameId), Name = "IX_articles_game_id")]
public partial class Article
{
    [Key]
    [Column("article_id")]
    public Guid ArticleId { get; set; }

    [Column("game_id")]
    public Guid GameId { get; set; }

    [Column("summary")]
    public string? Summary { get; set; }

    [Column("content_json")]
    public string? ContentJson { get; set; }

    [Column("created_at", TypeName = "datetime2(0)")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", TypeName = "datetime2(0)")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey(nameof(GameId))]
    [InverseProperty(nameof(Game.Articles))]
    public virtual Game Game { get; set; } = null!;
}
