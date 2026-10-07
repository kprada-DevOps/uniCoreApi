using System.Text.Json;

namespace UniCore.Api.Middleware;

/// <summary>
/// Manejo global de excepciones. Captura cualquier error no controlado,
/// lo registra y devuelve una respuesta JSON uniforme en lugar de filtrar
/// detalles internos al cliente.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
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

        // Solo se traducen errores cuya causa es claramente del cliente.
        // El resto se reporta como 500 sin filtrar detalles de configuracion o infraestructura.
        var statusCode = exception switch
        {
            KeyNotFoundException => StatusCodes.Status404NotFound,
            UnauthorizedAccessException => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError,
        };

        _logger.LogError(exception, "Error no controlado procesando {Method} {Path}",
            context.Request.Method, context.Request.Path);

        var mensaje = statusCode == StatusCodes.Status500InternalServerError
            ? "Error interno del servidor"
            : exception.Message;

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            mensaje,
            tipomensaje = Helpers.TipoRespuesta.error,
        }));
    }
}
