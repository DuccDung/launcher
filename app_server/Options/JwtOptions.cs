namespace app_server.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "app_server";

    public string Audience { get; set; } = "app_client";

    public string SecretKey { get; set; } = "CHANGE_THIS_TO_A_LONG_RANDOM_SECRET_KEY_1234567890";

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 7;
}
