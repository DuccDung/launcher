using Microsoft.EntityFrameworkCore;

namespace app_server.Models;

public partial class LauncherDbContext
{
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<UserOtp> UserOtps { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserOtp>(entity =>
        {
            entity.Property(e => e.UserOtpId).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Email).IsUnicode(false);
            entity.Property(e => e.OtpCode).IsUnicode(false);
            entity.Property(e => e.Purpose).IsUnicode(false);

            entity.HasOne(d => d.User)
                .WithMany(p => p.UserOtps)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_user_otps_users");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.Property(e => e.RefreshTokenId).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.TokenHash).IsUnicode(false);

            entity.HasOne(d => d.User)
                .WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_refresh_tokens_users");
        });

        ConfigureGameCatalogModels(modelBuilder);
        ConfigureCommerceModels(modelBuilder);
        ConfigureAuthorizationModels(modelBuilder);
    }
}
