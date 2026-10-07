namespace UniCore.Api.Configuration;

/// <summary>
/// Opciones de CORS. Se enlazan con la sección "Cors" de appsettings.
/// </summary>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    /// <summary>
    /// Orígenes permitidos. Vacío significa que no se permite ningún origen
    /// salvo los que se agreguen explícitamente.
    /// </summary>
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}
