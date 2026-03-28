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
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql);
    }
}
