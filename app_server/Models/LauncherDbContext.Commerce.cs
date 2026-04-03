using Microsoft.EntityFrameworkCore;

namespace app_server.Models;

public partial class LauncherDbContext
{
    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    private static void ConfigureCommerceModels(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cart>(entity =>
        {
            entity.Property(e => e.CartId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Status).HasDefaultValue("active");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.User)
                .WithOne(p => p.Cart)
                .HasForeignKey<Cart>(d => d.UserId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_carts_users");
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.ToTable(tableBuilder =>
                tableBuilder.HasCheckConstraint("CK_cart_items_quantity", "[quantity] > 0"));

            entity.Property(e => e.CartItemId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Quantity).HasDefaultValue(1);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Cart)
                .WithMany(p => p.CartItems)
                .HasForeignKey(d => d.CartId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_cart_items_carts");

            entity.HasOne(d => d.Game)
                .WithMany(p => p.CartItems)
                .HasForeignKey(d => d.GameId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_cart_items_games");
        });
    }
}
