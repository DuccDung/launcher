using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace app_server.Models;

[Table("game_files")]
[Index(nameof(GameId), Name = "IX_game_files_game_id")]
public partial class GameFile
{
    [Key]
    [Column("file_id")]
    public Guid FileId { get; set; }

    [Column("game_id")]
    public Guid GameId { get; set; }

    [Column("file_url_01")]
    [StringLength(1000)]
    public string? FileUrl01 { get; set; }

    [Column("file_url_02")]
    [StringLength(1000)]
    public string? FileUrl02 { get; set; }

    [Column("file_url_03")]
    [StringLength(1000)]
    public string? FileUrl03 { get; set; }

    [Column("file_url_04")]
    [StringLength(1000)]
    public string? FileUrl04 { get; set; }

    [Column("file_url_05")]
    [StringLength(1000)]
    public string? FileUrl05 { get; set; }

    [Column("file_type")]
    [StringLength(100)]
    public string? FileType { get; set; }

    [Column("created_at", TypeName = "datetime2(0)")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", TypeName = "datetime2(0)")]
    public DateTime UpdatedAt { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [ForeignKey(nameof(GameId))]
    [InverseProperty(nameof(Game.GameFiles))]
    public virtual Game Game { get; set; } = null!;

    [InverseProperty(nameof(Media.Asset))]
    public virtual ICollection<Media> MediaItems { get; set; } = new List<Media>();
}
