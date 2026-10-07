using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniCore.Api.Helpers;
using UniCore.Api.Modules.Personas.Managers;
using UniCore.Api.Modules.Personas.Requests;

namespace UniCore.Api.Modules.Personas.Controllers;

/// <summary>
/// Personas del sistema. Entidad base de estudiantes y docentes.
/// No hay borrado fisico: DELETE es baja logica.
/// </summary>
[ApiController]
[Route("{conexion}/[controller]")]
[Authorize]
public sealed class PersonaController : ControllerBase
{
    private readonly PersonaManager _personaManager;
    private readonly ILogger<PersonaController> _logger;

    public PersonaController(PersonaManager personaManager, ILogger<PersonaController> logger)
    {
        _personaManager = personaManager;
        _logger = logger;
    }

    /// <summary>
    /// Personas, por omision solo las activas.
    /// </summary>
    [HttpGet("ObtenerPersonas")]
    public async Task<ActionResult> ObtenerPersonas(
        string conexion,
        [FromQuery] bool incluirInactivas = false,
        [FromQuery] string? busqueda = null)
    {
        var personas = await _personaManager.ObtenerPersonas(conexion, incluirInactivas, busqueda);
        return Respuesta.Success(personas);
    }

    [HttpGet("ObtenerPersona/{cod:long:min(1)}")]
    public async Task<ActionResult> ObtenerPersona(string conexion, long cod)
    {
        var persona = await _personaManager.ObtenerPersona(cod, conexion);
        if (persona is null)
            return Respuesta.NotFound($"No existe una persona con el código {cod}");

        return Respuesta.Success(persona);
    }

    /// <summary>
    /// Da de alta una persona y devuelve su cod.
    /// Si el par tipo de documento y numero ya existe, la violacion del indice unico
    /// uq_per_persona_documento la traduce el middleware global a 409.
    /// </summary>
    [HttpPost("CrearPersona")]
    public async Task<ActionResult> CrearPersona(string conexion, [FromBody] PersonaRequest request)
    {
        var cod = await _personaManager.CrearPersona(request, conexion);
        _logger.LogInformation("Persona creada con cod {Cod} en la conexión {Conexion}", cod, conexion);

        return Respuesta.Success(cod, "Persona creada correctamente");
    }

    /// <summary>
    /// Reemplaza los datos de una persona. Poner activo en true reactiva una
    /// persona dada de baja.
    /// </summary>
    [HttpPut("ActualizarPersona/{cod:long:min(1)}")]
    public async Task<ActionResult> ActualizarPersona(string conexion, long cod, [FromBody] PersonaActualizarRequest request)
    {
        var actualizada = await _personaManager.ActualizarPersona(cod, request, conexion);
        if (!actualizada)
            return Respuesta.NotFound($"No existe una persona con el código {cod}");

        return Respuesta.Success(true, "Persona actualizada correctamente");
    }

    /// <summary>
    /// Baja logica. No borra la fila: marca activo en 0, porque estudiantes, docentes,
    /// usuarios y registros de otros sectores la referencian y el esquema no declara
    /// ningun ON DELETE.
    /// </summary>
    [HttpDelete("{cod:long:min(1)}")]
    public async Task<ActionResult> EliminarPersona(string conexion, long cod)
    {
        var desactivada = await _personaManager.DesactivarPersona(cod, conexion);
        if (!desactivada)
            return Respuesta.NotFound($"No existe una persona con el código {cod}");

        return Respuesta.Success(true, "Persona desactivada correctamente");
    }
}
