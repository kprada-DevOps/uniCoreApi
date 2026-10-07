using System.Collections.Concurrent;

namespace UniCore.Api.Database;

/// <summary>
/// Resuelve y cachea los valores de configuración de base de datos.
/// Estructura esperada en appsettings.json:
/// "ConnectionStrings": { "&lt;nombre&gt;": { "conexion": "...", "zonahoraria": "..." } }
/// </summary>
public static class AppSettingsCache
{
    private static readonly ConcurrentDictionary<string, string?> _connectionStringCache = new();
    private static readonly ConcurrentDictionary<string, string?> _zonaHorariaCache = new();

    /// <summary>
    /// Devuelve la cadena de conexión de la base de datos indicada, o null si no existe.
    /// </summary>
    public static string? GetConnectionString(string conexion)
    {
        if (_connectionStringCache.TryGetValue(conexion, out var cached) && cached != null)
            return cached;

        var value = ConfigurationService.Configuration["ConnectionStrings:" + conexion + ":conexion"];
        if (value != null)
            _connectionStringCache[conexion] = value;

        return value;
    }

    public static bool HasConnection(string conexion)
    {
        return GetConnectionString(conexion) != null;
    }

    /// <summary>
    /// Devuelve el identificador de zona horaria configurada para la conexión indicada.
    /// </summary>
    public static string? GetZonaHoraria(string conexion)
    {
        if (_zonaHorariaCache.TryGetValue(conexion, out var cached) && cached != null)
            return cached;

        var value = ConfigurationService.Configuration["ConnectionStrings:" + conexion + ":zonahoraria"];
        if (value != null)
            _zonaHorariaCache[conexion] = value;

        return value;
    }

    /// <summary>
    /// Nombres de todas las conexiones declaradas en la configuración.
    /// </summary>
    public static IEnumerable<string> GetConnectionNames()
    {
        var section = ConfigurationService.Configuration.GetSection("ConnectionStrings");
        return section.GetChildren()
            .Where(child => child.GetChildren().Any())
            .Select(child => child.Key)
            .OrderBy(key => key, StringComparer.Ordinal);
    }

    /// <summary>
    /// Vacía la caché. Útil en pruebas o cuando la configuración cambia en caliente.
    /// </summary>
    public static void Clear()
    {
        _connectionStringCache.Clear();
        _zonaHorariaCache.Clear();
    }
}
