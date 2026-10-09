using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniCore.Api.Helpers;
using UniCore.Api.Modules.PlanEstudios.Managers;
using UniCore.Api.Modules.PlanEstudios.Requests;
using UniCore.Api.Modules.Seguridad.Managers;

namespace UniCore.Api.Modules.PlanEstudios.Controllers;

[ApiController, Route("{conexion}/PlanEstudios/RequisitosCocurriculares"), Authorize]
public sealed class RequisitosCocurricularesController : ControllerBase
{
    private readonly RequisitosCocurricularesManager _manager;
    private readonly AuditoriaManager _audit;

    public RequisitosCocurricularesController(RequisitosCocurricularesManager manager, AuditoriaManager audit)
    {
        _manager = manager;
        _audit = audit;
    }
    [HttpGet("ObtenerRequisitosCocurriculares"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.CONSULTAR")]
    public async Task<ActionResult> Listar(string conexion, [FromQuery] bool incluirInactivos = false, [FromQuery] string? busqueda = null)
        => Respuesta.Success(await _manager.Listar(conexion, incluirInactivos, busqueda));
    [HttpGet("ObtenerRequisitoCocurricular/{cod:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.CONSULTAR")]
    public async Task<ActionResult> Obtener(string conexion, int cod)
    { var item = await _manager.Obtener(conexion, cod); return item is null ? Respuesta.NotFound("El requisito cocurricular no existe.") : Respuesta.Success(item); }
    [HttpPost("CrearRequisitoCocurricular"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> Crear(string conexion, [FromBody] RequisitoCocurricularRequest r)
    {
        r.codigo = r.codigo.Trim().ToUpperInvariant();
        if (await _manager.ExisteCodigo(conexion, r.codigo, null) is not null) return Respuesta.Conflict("El código del requisito ya existe.");
        if (!await _manager.ReferenciasValidas(conexion, r)) return Respuesta.Failed<object>(mensaje: "El tipo, comportamiento, unidad de medida o estado no es válido.");
        var cod = await _manager.Crear(conexion, r); await _audit.Registrar(conexion, "aca_requisitos_cocurriculares", cod.ToString(), "CREAR", nuevo: r);
        return Respuesta.Success(await _manager.Obtener(conexion, cod), "Requisito creado correctamente.");
    }
    [HttpPut("ActualizarRequisitoCocurricular/{cod:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> Actualizar(string conexion, int cod, [FromBody] RequisitoCocurricularRequest r)
    {
        var antes = await _manager.Obtener(conexion, cod); if (antes is null) return Respuesta.NotFound("El requisito cocurricular no existe.");
        r.codigo = r.codigo.Trim().ToUpperInvariant();
        if (await _manager.ExisteCodigo(conexion, r.codigo, cod) is not null) return Respuesta.Conflict("El código del requisito ya existe.");
        if (!await _manager.ReferenciasValidas(conexion, r)) return Respuesta.Failed<object>(mensaje: "El tipo, comportamiento, unidad de medida o estado no es válido.");
        await _manager.Actualizar(conexion, cod, r); await _audit.Registrar(conexion, "aca_requisitos_cocurriculares", cod.ToString(), "EDITAR", antes, r);
        return Respuesta.Success(await _manager.Obtener(conexion, cod), "Requisito actualizado correctamente.");
    }
    [HttpPut("CambiarEstadoRequisitoCocurricular/{cod:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> Estado(string conexion, int cod, [FromQuery] int cod_estado)
    {
        var antes = await _manager.Obtener(conexion, cod); if (antes is null) return Respuesta.NotFound("El requisito cocurricular no existe.");
        if (!await _manager.EstadoExiste(conexion, cod_estado)) return Respuesta.Failed<object>(mensaje: "El estado no existe o está inactivo.");
        await _manager.CambiarEstado(conexion, cod, cod_estado); await _audit.Registrar(conexion, "aca_requisitos_cocurriculares", cod.ToString(), "CAMBIAR_ESTADO", antes, new { cod_estado });
        return Respuesta.Success(await _manager.Obtener(conexion, cod), "Estado actualizado.");
    }
    [HttpPut("GuardarMecanismosRequisito/{cod:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> Mecanismos(string conexion, int cod, [FromBody] ReemplazarRelacionesRequest<MecanismoRequisitoRequest> r)
    {
        var antes = await _manager.Obtener(conexion, cod); if (antes is null) return Respuesta.NotFound("El requisito cocurricular no existe.");
        try { await _manager.ReemplazarMecanismos(conexion, cod, r.items); } catch (InvalidOperationException ex) { return Respuesta.Failed<object>(mensaje: ex.Message); }
        var despues = await _manager.Obtener(conexion, cod); await _audit.Registrar(conexion, "aca_requisito_mecanismos", cod.ToString(), "REEMPLAZAR", antes.mecanismos, despues?.mecanismos);
        return Respuesta.Success(despues, "Mecanismos actualizados.");
    }
    [HttpPut("GuardarIdiomasRequisito/{cod:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> Idiomas(string conexion, int cod, [FromBody] ReemplazarRelacionesRequest<RequisitoIdiomaRequest> r)
    {
        var antes = await _manager.Obtener(conexion, cod); if (antes is null) return Respuesta.NotFound("El requisito cocurricular no existe.");
        try { await _manager.ReemplazarIdiomas(conexion, cod, r.items); } catch (InvalidOperationException ex) { return Respuesta.Failed<object>(mensaje: ex.Message); }
        var despues = await _manager.Obtener(conexion, cod); await _audit.Registrar(conexion, "aca_requisito_idiomas", cod.ToString(), "REEMPLAZAR", antes.idiomas, despues?.idiomas);
        return Respuesta.Success(despues, "Configuración de idiomas actualizada.");
    }
    [HttpPut("GuardarActividadesRequisito/{cod:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> Actividades(string conexion, int cod, [FromBody] ReemplazarRelacionesRequest<RequisitoActividadRequest> r)
    {
        var antes = await _manager.Obtener(conexion, cod); if (antes is null) return Respuesta.NotFound("El requisito cocurricular no existe.");
        try { await _manager.ReemplazarActividades(conexion, cod, r.items.Select(x => x.cod_actividad).ToArray()); } catch (InvalidOperationException ex) { return Respuesta.Failed<object>(mensaje: ex.Message); }
        var despues = await _manager.Obtener(conexion, cod); await _audit.Registrar(conexion, "aca_requisito_actividades", cod.ToString(), "REEMPLAZAR", antes.actividades, despues?.actividades);
        return Respuesta.Success(despues, "Actividades actualizadas.");
    }
}
