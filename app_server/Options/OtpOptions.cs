namespace app_server.Options;

public class OtpOptions
{
    public const string SectionName = "Otp";

    public int ExpireMinutes { get; set; } = 5;

    public int PasswordResetExpireMinutes { get; set; } = 10;
}
