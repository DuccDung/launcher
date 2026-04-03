using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace app_server.Models;

[Table("cart_items")]
[Index(nameof(CartId), Name = "IX_cart_items_cart_id")]
[Index(nameof(GameId), Name = "IX_cart_items_game_id")]
[Index(nameof(CartId), nameof(GameId), Name = "UQ_cart_items_cart_game", IsUnique = true)]
public partial class CartItem
{
    [Key]
    [Column("cart_item_id")]
    public Guid CartItemId { get; set; }

    [Column("cart_id")]
    public Guid CartId { get; set; }

    [Column("game_id")]
    public Guid GameId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", TypeName = "datetime")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey(nameof(CartId))]
    [InverseProperty(nameof(Cart.CartItems))]
    public virtual Cart Cart { get; set; } = null!;

    [ForeignKey(nameof(GameId))]
    [InverseProperty(nameof(Game.CartItems))]
    public virtual Game Game { get; set; } = null!;
}
