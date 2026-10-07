using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace UniCore.Api.Helpers;

/// <summary>
/// Tipos de mensaje que viajan en <c>tipomensaje</c>.
/// Vocabulario identico al del proyecto de referencia.
/// </summary>
public enum TipoRespuesta
{
    success,
    info,
    warning,
    error,
}

/// <summary>
/// Envoltura uniforme para las respuestas de la API.
///
/// El cuerpo es exactamente el del proyecto de referencia:
/// <c>{ data, mensaje, tipomensaje }</c> en exito y
/// <c>{ mensaje, tipomensaje }</c> en error.
/// </summary>
public static class Respuesta
{
    public static ActionResult Success<T>(T data, string? mensaje = null, TipoRespuesta tipomensaje = TipoRespuesta.success)
        => new OkObjectResult(new { data, mensaje, tipomensaje });

    public static ActionResult Failed<T>(T? data = default, string mensaje = "La solicitud ha fallado", TipoRespuesta tipomensaje = TipoRespuesta.error)
        => new BadRequestObjectResult(new { data, mensaje, tipomensaje });

    public static ActionResult UnAuthorized(string mensaje = "No autorizado, debe iniciar sesión", TipoRespuesta tipomensaje = TipoRespuesta.error)
        => new UnauthorizedObjectResult(new { mensaje, tipomensaje });

    public static ActionResult Forbidden(string mensaje = "Acceso al recurso denegado", TipoRespuesta tipomensaje = TipoRespuesta.error)
        => new ObjectResult(new { mensaje, tipomensaje }) { StatusCode = StatusCodes.Status403Forbidden };
}

/// <summary>
/// Normaliza los errores de validación de modelo al formato de <see cref="Respuesta"/>,
/// para que el cliente reciba siempre la misma envoltura.
/// </summary>
public sealed class ValidationProblemDetailsFilter : IAsyncActionFilter
{
    private readonly ILogger<ValidationProblemDetailsFilter> _logger;

    public ValidationProblemDetailsFilter(ILogger<ValidationProblemDetailsFilter> logger)
    {
        _logger = logger;
    }

    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.ModelState.IsValid)
        {
            var errores = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

            var mensaje = string.Join(" | ", errores.Select(e => $"{e.Key}: {string.Join(", ", e.Value)}"));

            _logger.LogWarning("Peticion invalida en {Path}: {Mensaje}", context.HttpContext.Request.Path, mensaje);

            context.Result = new BadRequestObjectResult(new
            {
                mensaje,
                tipomensaje = TipoRespuesta.error,
                errores,
            });

            return Task.CompletedTask;
        }

        return next();
    }
}
