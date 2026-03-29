using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace app_server.Models;

[Table("categories")]
[Index(nameof(Slug), Name = "UX_categories_slug", IsUnique = true)]
public partial class Category
{
    [Key]
    [Column("category_id")]
    public Guid CategoryId { get; set; }

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("slug")]
    [StringLength(255)]
    public string? Slug { get; set; }

    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = null!;

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    [Column("short_description")]
    [StringLength(1000)]
    public string? ShortDescription { get; set; }

    [Column("created_at", TypeName = "datetime2(0)")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", TypeName = "datetime2(0)")]
    public DateTime UpdatedAt { get; set; }

    [InverseProperty(nameof(GameCategory.Category))]
    public virtual ICollection<GameCategory> GameCategories { get; set; } = new List<GameCategory>();
}
