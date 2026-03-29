using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace app_server.Models;

[Table("media")]
[Index(nameof(AssetId), Name = "IX_media_asset_id")]
public partial class Media
{
    [Key]
    [Column("media_id")]
    public Guid MediaId { get; set; }

    [Column("asset_id")]
    public Guid? AssetId { get; set; }

    [Column("url")]
    [StringLength(1000)]
    public string Url { get; set; } = null!;

    [Column("media_type")]
    [StringLength(100)]
    public string? MediaType { get; set; }

    [Column("created_at", TypeName = "datetime2(0)")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", TypeName = "datetime2(0)")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey(nameof(AssetId))]
    [InverseProperty(nameof(GameFile.MediaItems))]
    public virtual GameFile? Asset { get; set; }
}
