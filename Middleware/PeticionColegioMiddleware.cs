using System.Security.Claims;

namespace UniCore.Api.Middleware;

/// <summary>
/// Verifica que la conexión (tenant) de la ruta coincida con la indicada en el token JWT.
/// Si la petición no está autenticada, la deja pasar: la decisión corresponde al
/// esquema de autorización de cada endpoint.
/// </summary>
public sealed class PeticionColegioMiddleware
{
    /// <summary>
    /// Nombre del claim que transporta la conexión del usuario autenticado.
    /// </summary>
    public const string ConexionClaimType = "conexion";

    private readonly RequestDelegate _next;
    private readonly ILogger<PeticionColegioMiddleware> _logger;

    public PeticionColegioMiddleware(RequestDelegate next, ILogger<PeticionColegioMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var conexionRuta = CadenaConnectMiddleware.GetConnection(context);
        if (string.IsNullOrWhiteSpace(conexionRuta))
        {
            await _next(context);
            return;
        }

        var conexionToken = context.User.FindFirst(ConexionClaimType)?.Value;

        // Sin claim de conexión no hay nada que contrastar.
        if (string.IsNullOrWhiteSpace(conexionToken))
        {
            await _next(context);
            return;
        }

        if (!string.Equals(conexionRuta, conexionToken, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "La conexión de la ruta '{ConexionRuta}' no coincide con la del token '{ConexionToken}'.",
                conexionRuta, conexionToken);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsync(
                """{"success":false,"tipo":"error","mensaje":"La conexion no pertenece a la peticion"}""");
            return;
        }

        await _next(context);
    }
}
