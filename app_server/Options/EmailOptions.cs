namespace app_server.Options;

public class EmailOptions
{
    public const string SectionName = "Email";

    public string BrandName { get; set; } = "Deluxe Gaming";

    public string FromName { get; set; } = "Deluxe Gaming";

    public string FromAddress { get; set; } = string.Empty;
}
