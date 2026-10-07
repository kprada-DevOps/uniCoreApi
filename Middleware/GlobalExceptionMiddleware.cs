using System.Text.Json;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using UniCore.Api.Helpers;

namespace UniCore.Api.Middleware;

/// <summary>
/// Manejo global de excepciones. Captura cualquier error no controlado,
/// lo registra y devuelve una respuesta JSON uniforme en lugar de filtrar
/// detalles internos al cliente.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    // Codigos de MySQL que describen un error del cliente o de las reglas del negocio,
    // no un fallo del servidor. El esquema de UniCore no declara ningun ON DELETE, asi
    // que todo es RESTRICT y estos errores son el dia a dia, no una excepcion rara.
    private const int ErrorClaveDuplicada = 1062;
    private const int ErrorFilaReferenciada = 1451;
    private const int ErrorFilaPadreAusente = 1452;

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    /// <summary>
    /// Opciones de serializacion de MVC, las mismas que registra Program.cs con
    /// AddJsonOptions. Se necesitan para que la envoltura de error salga identica a la
    /// de exito: sin ellas, JsonSerializer.Serialize usaria las opciones por defecto
    /// y tipomensaje saldria como numero (3) en lugar de la cadena "error", que es lo
    /// que espera el frontend para distinguir un error de un exito.
    /// </summary>
    private readonly JsonSerializerOptions _jsonOptions;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IOptions<Microsoft.AspNetCore.Mvc.JsonOptions> jsonOptions)
    {
        _next = next;
        _logger = logger;
        _jsonOptions = jsonOptions.Value.JsonSerializerOptions;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // El cliente abortó la petición: no es un error de la aplicación.
            _logger.LogInformation("Petición cancelada por el cliente: {Path}", context.Request.Path);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogError(exception, "Error tras iniciar la respuesta: {Path}", context.Request.Path);
            throw exception;
        }

        var (statusCode, mensaje) = Traducir(exception);

        // El log conserva siempre el detalle completo, aunque al cliente solo le llegue
        // un mensaje generico: el texto de MySQL nombra la constraint y el valor que
        // colisiono, y eso es imprescindible para diagnosticar.
        _logger.LogError(exception, "Error no controlado procesando {Method} {Path}. MySQL: {Codigo}",
            context.Request.Method, context.Request.Path,
            (exception as MySqlException)?.Number);

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            mensaje,
            tipomensaje = TipoRespuesta.error,
        }, _jsonOptions));
    }

    /// <summary>
    /// Traduce la excepcion a un codigo HTTP y un mensaje. Solo se traducen errores
    /// cuya causa es claramente del cliente; el resto se reporta como 500 sin filtrar
    /// detalles de configuracion o infraestructura.
    /// </summary>
    private static (int StatusCode, string Mensaje) Traducir(Exception exception)
    {
        if (exception is MySqlException mySqlException)
        {
            var codigoMySql = mySqlException.Number;
            return codigoMySql switch
            {
                // Indice unico violado, por ejemplo uq_per_persona_documento.
                ErrorClaveDuplicada => (StatusCodes.Status409Conflict,
                    "Ya existe un registro con esos datos"),

                // FK RESTRICT: la fila referenciada tiene dependientes. Con la baja
                // logica por omision, solo se ve al intentar un borrado fisico.
                ErrorFilaReferenciada => (StatusCodes.Status409Conflict,
                    "No se puede completar la operación porque hay registros relacionados"),

                // FK sin fila padre: el cuerpo referencia un codigo que no existe.
                ErrorFilaPadreAusente => (StatusCodes.Status400BadRequest,
                    "La operación hace referencia a un registro que no existe"),

                _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor"),
            };
        }

        return exception switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor"),
        };
    }
}
