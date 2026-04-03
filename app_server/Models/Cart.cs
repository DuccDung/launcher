using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace app_server.Models;

[Table("carts")]
[Index(nameof(UserId), Name = "UQ_carts_user", IsUnique = true)]
public partial class Cart
{
    [Key]
    [Column("cart_id")]
    public Guid CartId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("status")]
    [StringLength(20)]
    public string Status { get; set; } = null!;

    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", TypeName = "datetime")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey(nameof(UserId))]
    [InverseProperty(nameof(User.Cart))]
    public virtual User User { get; set; } = null!;

    [InverseProperty(nameof(CartItem.Cart))]
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}
