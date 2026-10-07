using Microsoft.Extensions.Configuration;

namespace UniCore.Api.Database;

/// <summary>
/// Puente entre el contenedor de configuración de ASP.NET Core y la capa de datos estática.
/// <see cref="AppSettingsCache"/> se mantiene estático por fidelidad con la referencia, así que
/// necesita una única fuente de configuración accessible sin inyectar dependencias.
/// </summary>
public static class ConfigurationService
{
    private static IConfiguration? _configuration;

    /// <summary>
    /// IConfiguration registrada durante el arranque de la aplicación.
    /// </summary>
    public static IConfiguration Configuration =>
        _configuration ?? throw new InvalidOperationException(
            "La configuración no ha sido inicializada. Debe llamarse a " +
            "AddDatabaseInfrastructure desde Program.cs durante el arranque.");

    public static void Initialize(IConfiguration configuration)
    {
        _configuration = configuration;
        AppSettingsCache.Clear();
    }
}
