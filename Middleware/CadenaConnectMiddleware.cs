using UniCore.Api.Database;

namespace UniCore.Api.Middleware;

/// <summary>
/// Resuelve la conexión (tenant) a partir del primer segmento de la ruta y la expone
/// en <see cref="HttpContext.Items"/> bajo la clave <see cref="ConnectionItemKey"/>.
///
/// Igual que en el proyecto de referencia, todas las rutas llevan conexión, incluido
/// el login. Solo se eximen las rutas técnicas sin endpoint asociado (swagger, health).
/// </summary>
public sealed class CadenaConnectMiddleware
{
    public const string ConnectionItemKey = "unicore.conexion";

    /// <summary>
    /// Rutas técnicas exentas de conexión, sin endpoint asociado (health, swagger).
    /// </summary>
    private static readonly string[] PrefijosTecnicos =
    {
        "/swagger",
        "/health",
    };

    private readonly RequestDelegate _next;

    public CadenaConnectMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (EsRutaTecnica(context.Request.Path.Value))
        {
            await _next(context);
            return;
        }

        var conexion = ResolveConnectionFromPath(context.Request.Path.Value);

        // Sin conexión en la ruta, nada que validar.
        if (string.IsNullOrWhiteSpace(conexion))
        {
            await _next(context);
            return;
        }

        if (!AppSettingsCache.HasConnection(conexion))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync("Conexion invalida");
            return;
        }

        context.Items[ConnectionItemKey] = conexion;
        await _next(context);
    }

    private static bool EsRutaTecnica(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;

        var normalizada = path.TrimEnd('/');
        if (normalizada.Length == 0) return true;

        return PrefijosTecnicos.Any(prefijo =>
            normalizada.Equals(prefijo, StringComparison.OrdinalIgnoreCase)
            || normalizada.StartsWith(prefijo + "/", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Extrae el nombre de conexión del primer segmento de una ruta con formato
    /// "/{conexion}/{controller}/{accion}". Devuelve null si no hay ese segmento.
    /// </summary>
    private static string? ResolveConnectionFromPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2) return null;

        return segments[0];
    }

    /// <summary>
    /// Obtiene la conexión resuelta para la petición actual, si la hubo.
    /// </summary>
    public static string? GetConnection(HttpContext context)
        => context.Items.TryGetValue(ConnectionItemKey, out var value) ? value as string : null;
}
