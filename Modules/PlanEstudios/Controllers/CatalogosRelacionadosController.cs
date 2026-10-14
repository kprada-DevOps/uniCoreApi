using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniCore.Api.Helpers;
using UniCore.Api.Modules.PlanEstudios.Managers;
using UniCore.Api.Modules.PlanEstudios.Requests;
using UniCore.Api.Modules.Seguridad.Managers;

namespace UniCore.Api.Modules.PlanEstudios.Controllers;

[ApiController, Route("{conexion}/PlanEstudios"), Authorize]
public sealed class CatalogosRelacionadosController : ControllerBase
{
    private readonly CatalogosRelacionadosManager _manager;
    private readonly AuditoriaManager _audit;

    public CatalogosRelacionadosController(CatalogosRelacionadosManager manager, AuditoriaManager audit)
    {
        _manager = manager;
        _audit = audit;
    }

    [HttpGet("ObtenerActividadesCocurriculares"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.CONSULTAR")]
    public async Task<ActionResult> Actividades(string conexion, [FromQuery] bool incluirInactivos = false) =>
        Respuesta.Success(await _manager.Actividades(conexion, incluirInactivos));

    [HttpPost("CrearActividadCocurricular"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> CrearActividad(string conexion, [FromBody] ActividadCocurricularRequest request)
    {
        request.codigo = request.codigo.Trim().ToUpperInvariant();
        if (await _manager.ExisteCodigoActividad(request.codigo, conexion) is not null)
            return Respuesta.Conflict("El código de actividad ya existe.");
        if (!await _manager.ReferenciasValidas(request, conexion))
            return Respuesta.Failed<object>(mensaje: "El tipo de actividad o estado no es válido.");
        var cod = await _manager.CrearActividad(conexion, request);
        await _audit.Registrar(conexion, "aca_actividades_cocurriculares", cod.ToString(), "CREAR", nuevo: request);
        return Respuesta.Success(await _manager.Actividades(conexion, true), "Actividad creada.");
    }

    [HttpPut("ActualizarActividadCocurricular/{cod:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> ActualizarActividad(string conexion, int cod, [FromBody] ActividadCocurricularRequest request)
    {
        if (!await _manager.ExisteActividad(cod, conexion))
            return Respuesta.NotFound("La actividad no existe.");
        request.codigo = request.codigo.Trim().ToUpperInvariant();
        if (await _manager.ExisteCodigoActividad(request.codigo, conexion, cod) is not null)
            return Respuesta.Conflict("El código de actividad ya existe.");
        if (!await _manager.ReferenciasValidas(request, conexion))
            return Respuesta.Failed<object>(mensaje: "El tipo de actividad o estado no es válido.");
        await _manager.ActualizarActividad(conexion, cod, request);
        await _audit.Registrar(conexion, "aca_actividades_cocurriculares", cod.ToString(), "EDITAR", nuevo: request);
        return Respuesta.Success(await _manager.Actividades(conexion, true), "Actividad actualizada.");
    }
}
