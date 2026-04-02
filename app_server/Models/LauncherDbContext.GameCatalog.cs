using Microsoft.EntityFrameworkCore;

namespace app_server.Models;

public partial class LauncherDbContext
{
    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Game> Games { get; set; }

    public virtual DbSet<GameCategory> GameCategories { get; set; }

    public virtual DbSet<GameVersion> GameVersions { get; set; }

    public virtual DbSet<Media> Media { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    private static void ConfigureGameCatalogModels(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Game>(entity =>
        {
            entity.Property(e => e.GameId).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.PhotoUrl).HasMaxLength(500).IsUnicode();
            entity.Property(e => e.IsRemove).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(e => e.CategoryId).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Status).HasDefaultValue("Published");
            entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<GameCategory>(entity =>
        {
            entity.Property(e => e.GameCategoryId).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Game)
                .WithMany(p => p.GameCategories)
                .HasForeignKey(d => d.GameId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_game_categories_games");

            entity.HasOne(d => d.Category)
                .WithMany(p => p.GameCategories)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_game_categories_categories");
        });

        modelBuilder.Entity<GameVersion>(entity =>
        {
            entity.Property(e => e.VersionId).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.VersionName).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsRemoved).HasDefaultValue(false);

            entity.HasOne(d => d.Game)
                .WithMany(p => p.GameVersions)
                .HasForeignKey(d => d.GameId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_game_versions_games");
        });

        modelBuilder.Entity<Media>(entity =>
        {
            entity.Property(e => e.MediaId).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Game)
                .WithMany(p => p.MediaItems)
                .HasForeignKey(d => d.GameId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_media_games");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.Property(e => e.ReviewId).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Game)
                .WithMany(p => p.Reviews)
                .HasForeignKey(d => d.GameId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_reviews_games");

            entity.HasOne(d => d.User)
                .WithMany(p => p.Reviews)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_reviews_users");
        });
    }
}
