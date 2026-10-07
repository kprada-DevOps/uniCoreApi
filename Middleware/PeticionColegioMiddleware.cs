using System.Security.Claims;
using UniCore.Api.Database;

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
    // AuthController emite la conexión autenticada bajo el claim `colegio`.
    public const string ConexionClaimType = "colegio";

    private readonly RequestDelegate _next;
    private readonly ILogger<PeticionColegioMiddleware> _logger;

    public PeticionColegioMiddleware(RequestDelegate next, ILogger<PeticionColegioMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, DatabaseProvider database)
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

        // Un token autenticado sin conexión no puede autorizar una ruta tenant.
        if (string.IsNullOrWhiteSpace(conexionToken))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsync(
                "{\"mensaje\":\"El token no contiene una conexión válida\",\"tipomensaje\":\"error\"}");
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
                "{\"mensaje\":\"La conexión del token no coincide con la de la petición\",\"tipomensaje\":\"error\"}");
            return;
        }

        // El estado de usuario se consulta en la BD en cada petición protegida,
        // evitando que un access token siga sirviendo tras una desactivación.
        if (!long.TryParse(context.User.FindFirst("codusuario")?.Value, out var codUsuario))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }
        var activo = await database.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM seg_usuarios WHERE cod=@cod AND activo=1;", new { cod = codUsuario }, conexionRuta);
        if (activo == 0)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsync("{\"mensaje\":\"La cuenta está inactiva\",\"tipomensaje\":\"error\"}");
            return;
        }

        await _next(context);
    }
}
