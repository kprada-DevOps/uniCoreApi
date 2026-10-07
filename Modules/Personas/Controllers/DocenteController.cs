using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniCore.Api.Helpers;
using UniCore.Api.Modules.Personas.Managers;
using UniCore.Api.Modules.Personas.Requests;

namespace UniCore.Api.Modules.Personas.Controllers;

/// <summary>
/// Docentes. La persona debe existir antes: se referencia por cod_persona.
/// No hay borrado fisico: DELETE es baja logica.
/// </summary>
[ApiController]
[Route("{conexion}/[controller]")]
[Authorize]
public sealed class DocenteController : ControllerBase
{
    private readonly DocenteManager _docenteManager;
    private readonly ILogger<DocenteController> _logger;

    public DocenteController(DocenteManager docenteManager, ILogger<DocenteController> logger)
    {
        _docenteManager = docenteManager;
        _logger = logger;
    }

    [HttpGet("ObtenerDocentes")]
    public async Task<ActionResult> ObtenerDocentes(
        string conexion,
        [FromQuery] bool incluirInactivos = false,
        [FromQuery] string? busqueda = null)
    {
        var docentes = await _docenteManager.ObtenerDocentes(conexion, incluirInactivos, busqueda);
        return Respuesta.Success(docentes);
    }

    [HttpGet("ObtenerDocente/{cod:long:min(1)}")]
    public async Task<ActionResult> ObtenerDocente(string conexion, long cod)
    {
        var docente = await _docenteManager.ObtenerDocente(cod, conexion);
        if (docente is null)
            return Respuesta.NotFound($"No existe un docente con el código {cod}");

        return Respuesta.Success(docente);
    }

    /// <summary>
    /// Registra un docente y devuelve su cod. A diferencia del estudiante, la fecha
    /// de ingreso es opcional en esta tabla.
    /// </summary>
    [HttpPost("CrearDocente")]
    public async Task<ActionResult> CrearDocente(string conexion, [FromBody] DocenteRequest request)
    {
        var cod = await _docenteManager.CrearDocente(request, conexion);
        _logger.LogInformation("Docente creado con cod {Cod} en la conexión {Conexion}", cod, conexion);

        return Respuesta.Success(cod, "Docente creado correctamente");
    }

    [HttpPut("ActualizarDocente/{cod:long:min(1)}")]
    public async Task<ActionResult> ActualizarDocente(string conexion, long cod, [FromBody] DocenteActualizarRequest request)
    {
        var actualizado = await _docenteManager.ActualizarDocente(cod, request, conexion);
        if (!actualizado)
            return Respuesta.NotFound($"No existe un docente con el código {cod}");

        return Respuesta.Success(true, "Docente actualizado correctamente");
    }

    /// <summary>
    /// Baja logica: pasa el estado a INACTIVO. No borra la fila.
    /// </summary>
    [HttpDelete("{cod:long:min(1)}")]
    public async Task<ActionResult> EliminarDocente(string conexion, long cod)
    {
        var desactivado = await _docenteManager.DesactivarDocente(cod, conexion);
        if (!desactivado)
            return Respuesta.NotFound($"No existe un docente con el código {cod}");

        return Respuesta.Success(true, "Docente desactivado correctamente");
    }
}
