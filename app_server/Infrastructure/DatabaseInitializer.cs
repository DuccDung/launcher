using Microsoft.EntityFrameworkCore;

namespace app_server.Infrastructure;

public static class DatabaseInitializer
{
    public static async Task EnsureAuthTablesAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<app_server.Models.LauncherDbContext>();

        const string sql = """
            IF OBJECT_ID('dbo.refresh_tokens', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.refresh_tokens (
                    refresh_token_id uniqueidentifier NOT NULL
                        CONSTRAINT DF_refresh_tokens_refresh_token_id DEFAULT NEWSEQUENTIALID(),
                    user_id uniqueidentifier NOT NULL,
                    token_hash varchar(255) NOT NULL,
                    expires_at datetime2 NOT NULL,
                    created_at datetime2 NOT NULL
                        CONSTRAINT DF_refresh_tokens_created_at DEFAULT SYSDATETIME(),
                    revoked_at datetime2 NULL,
                    CONSTRAINT PK_refresh_tokens PRIMARY KEY (refresh_token_id),
                    CONSTRAINT FK_refresh_tokens_users
                        FOREIGN KEY (user_id) REFERENCES dbo.users(user_id)
                        ON DELETE CASCADE,
                    CONSTRAINT UQ_refresh_tokens_token_hash UNIQUE (token_hash)
                );

                CREATE INDEX IX_refresh_tokens_user_id ON dbo.refresh_tokens(user_id);
            END

            IF OBJECT_ID('dbo.user_otps', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.user_otps (
                    user_otp_id uniqueidentifier NOT NULL
                        CONSTRAINT DF_user_otps_user_otp_id DEFAULT NEWSEQUENTIALID(),
                    user_id uniqueidentifier NOT NULL,
                    email varchar(255) NOT NULL,
                    purpose varchar(50) NOT NULL,
                    otp_code varchar(12) NOT NULL,
                    expires_at datetime2 NOT NULL,
                    is_used bit NOT NULL
                        CONSTRAINT DF_user_otps_is_used DEFAULT ((0)),
                    created_at datetime2 NOT NULL
                        CONSTRAINT DF_user_otps_created_at DEFAULT SYSUTCDATETIME(),
                    used_at datetime2 NULL,
                    CONSTRAINT PK_user_otps PRIMARY KEY (user_otp_id),
                    CONSTRAINT FK_user_otps_users
                        FOREIGN KEY (user_id) REFERENCES dbo.users(user_id)
                        ON DELETE CASCADE
                );

                CREATE INDEX IX_user_otps_user_id ON dbo.user_otps(user_id);
                CREATE INDEX IX_user_otps_email_purpose ON dbo.user_otps(email, purpose);
            END

            IF OBJECT_ID('dbo.games', 'U') IS NOT NULL
               AND COL_LENGTH('dbo.games', 'steam_app_id') IS NULL
            BEGIN
                ALTER TABLE dbo.games
                ADD steam_app_id int NULL;

                CREATE INDEX IX_games_steam_app_id ON dbo.games(steam_app_id);
            END
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql);
    }
}
