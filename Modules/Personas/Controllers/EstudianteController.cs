using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniCore.Api.Helpers;
using UniCore.Api.Modules.Personas.Managers;
using UniCore.Api.Modules.Personas.Requests;

namespace UniCore.Api.Modules.Personas.Controllers;

/// <summary>
/// Estudiantes. La persona debe existir antes: se referencia por cod_persona.
/// No hay borrado fisico: DELETE es baja logica.
/// </summary>
[ApiController]
[Route("{conexion}/[controller]")]
[Authorize]
public sealed class EstudianteController : ControllerBase
{
    private readonly EstudianteManager _estudianteManager;
    private readonly ILogger<EstudianteController> _logger;

    public EstudianteController(EstudianteManager estudianteManager, ILogger<EstudianteController> logger)
    {
        _estudianteManager = estudianteManager;
        _logger = logger;
    }

    [Authorize(Policy = "PERMISO:ESTUDIANTES.CONSULTAR")]
    [HttpGet("ObtenerEstudiantes")]
    public async Task<ActionResult> ObtenerEstudiantes(
        string conexion,
        [FromQuery] bool incluirInactivos = false,
        [FromQuery] string? busqueda = null)
    {
        var estudiantes = await _estudianteManager.ObtenerEstudiantes(conexion, incluirInactivos, busqueda);
        return Respuesta.Success(estudiantes);
    }

    [Authorize(Policy = "PERMISO:ESTUDIANTES.CONSULTAR")]
    [HttpGet("ObtenerEstudiante/{cod:long:min(1)}")]
    public async Task<ActionResult> ObtenerEstudiante(string conexion, long cod)
    {
        var estudiante = await _estudianteManager.ObtenerEstudiante(cod, conexion);
        if (estudiante is null)
            return Respuesta.NotFound($"No existe un estudiante con el código {cod}");

        return Respuesta.Success(estudiante);
    }

    /// <summary>
    /// Inscribe un estudiante y devuelve la proyección completa creada. La persona debe existir: si
    /// cod_persona no corresponde a ninguna fila, la violacion de FK la traduce el
    /// middleware global a 400. Si esa persona ya esta inscrita, el indice unico
    /// uq_per_estudiante_persona produce un 409.
    /// </summary>
    [Authorize(Policy = "PERMISO:ESTUDIANTES.EDITAR")]
    [HttpPost("CrearEstudiante")]
    public async Task<ActionResult> CrearEstudiante(string conexion, [FromBody] EstudianteRequest request)
    {
        var cod = await _estudianteManager.CrearEstudiante(request, conexion);
        _logger.LogInformation("Estudiante creado con cod {Cod} en la conexión {Conexion}", cod, conexion);

        var estudiante = await _estudianteManager.ObtenerEstudiante(cod, conexion);
        return Respuesta.Success(estudiante, "Estudiante creado correctamente");
    }

    [Authorize(Policy = "PERMISO:ESTUDIANTES.EDITAR")]
    [HttpPut("ActualizarEstudiante/{cod:long:min(1)}")]
    public async Task<ActionResult> ActualizarEstudiante(string conexion, long cod, [FromBody] EstudianteActualizarRequest request)
    {
        var actualizado = await _estudianteManager.ActualizarEstudiante(cod, request, conexion);
        if (!actualizado)
            return Respuesta.NotFound($"No existe un estudiante con el código {cod}");

        var estudianteActualizado = await _estudianteManager.ObtenerEstudiante(cod, conexion);
        return Respuesta.Success(estudianteActualizado, "Estudiante actualizado correctamente");
    }

    /// <summary>
    /// Baja logica: pasa el estado a INACTIVO. No borra la fila ni toca el activo de
    /// la persona asociada.
    /// </summary>
    [Authorize(Policy = "PERMISO:ESTUDIANTES.CREAR")]
    [HttpDelete("{cod:long:min(1)}")]
    public async Task<ActionResult> EliminarEstudiante(string conexion, long cod)
    {
        var desactivado = await _estudianteManager.DesactivarEstudiante(cod, conexion);
        if (!desactivado)
            return Respuesta.NotFound($"No existe un estudiante con el código {cod}");

        return Respuesta.Success(true, "Estudiante desactivado correctamente");
    }
}
