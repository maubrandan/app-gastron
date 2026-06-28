namespace Resto.Infrastructure.Identity;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = "Resto.Api";

    public string Audience { get; set; } = "Resto.App";

    public int ExpiryMinutes { get; set; } = 480;
}
