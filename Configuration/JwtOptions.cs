namespace UniCore.Api.Configuration;

/// <summary>
/// Opciones de autenticación JWT. Se enlazan con la sección "Jwt" de appsettings.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string TokenSecret { get; set; } = string.Empty;

    public int ExpirationHours { get; set; } = 8;
}
