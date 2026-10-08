using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniCore.Api.Helpers;
using UniCore.Api.Modules.PlanEstudios.Managers;
using UniCore.Api.Modules.PlanEstudios.Requests;
using UniCore.Api.Modules.Seguridad.Managers;

namespace UniCore.Api.Modules.PlanEstudios.Controllers;

[ApiController, Route("{conexion}/PlanEstudios/Planes"), Authorize]
public sealed class PlanesEstudioController(PlanesEstudioManager manager, AuditoriaManager audit) : ControllerBase
{
    [HttpGet, Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.CONSULTAR")]
    public async Task<ActionResult> Listar(string conexion, [FromQuery] int? cod_programa = null, [FromQuery] bool incluirInactivos = false)
        => Respuesta.Success(await manager.Listar(conexion, cod_programa, incluirInactivos));
    [HttpGet("{cod:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.CONSULTAR")]
    public async Task<ActionResult> Obtener(string conexion, int cod)
    {
        var item = await manager.Obtener(conexion, cod); return item is null ? Respuesta.NotFound("El plan de estudios no existe.") : Respuesta.Success(item);
    }
    [HttpPost, Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> Crear(string conexion, [FromBody] PlanEstudioRequest r)
    {
        r.codigo = r.codigo.Trim().ToUpperInvariant();
        if (r.fecha_fin.HasValue && r.fecha_fin.Value.Date < r.fecha_inicio.Date) return Respuesta.Failed<object>(mensaje: "La fecha fin debe ser igual o posterior a la fecha inicio.");
        if (!await manager.ProgramaExiste(conexion, r.cod_programa) || !await manager.EstadoExiste(conexion, r.cod_estado)) return Respuesta.Failed<object>(mensaje: "El programa o estado del plan no es válido.");
        if (await manager.ExisteCodigo(conexion, r.cod_programa, r.codigo, null) is not null) return Respuesta.Conflict("El programa ya tiene un plan con ese código.");
        var cod = await manager.Crear(conexion, r); await audit.Registrar(conexion, "aca_planes_estudio", cod.ToString(), "CREAR", nuevo: r);
        return Respuesta.Success(await manager.Obtener(conexion, cod), "Plan creado correctamente.");
    }
    [HttpPut("{cod:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> Actualizar(string conexion, int cod, [FromBody] PlanEstudioRequest r)
    {
        var antes = await manager.Obtener(conexion, cod); if (antes is null) return Respuesta.NotFound("El plan de estudios no existe.");
        r.codigo = r.codigo.Trim().ToUpperInvariant();
        if (r.fecha_fin.HasValue && r.fecha_fin.Value.Date < r.fecha_inicio.Date) return Respuesta.Failed<object>(mensaje: "La fecha fin debe ser igual o posterior a la fecha inicio.");
        if (!await manager.ProgramaExiste(conexion, r.cod_programa) || !await manager.EstadoExiste(conexion, r.cod_estado)) return Respuesta.Failed<object>(mensaje: "El programa o estado del plan no es válido.");
        if (await manager.ExisteCodigo(conexion, r.cod_programa, r.codigo, cod) is not null) return Respuesta.Conflict("El programa ya tiene un plan con ese código.");
        await manager.Actualizar(conexion, cod, r); await audit.Registrar(conexion, "aca_planes_estudio", cod.ToString(), "EDITAR", antes, r);
        return Respuesta.Success(await manager.Obtener(conexion, cod), "Plan actualizado correctamente.");
    }
    [HttpPut("{cod:int:min(1)}/Estado"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> Estado(string conexion, int cod, [FromQuery] int cod_estado)
    {
        var antes = await manager.Obtener(conexion, cod); if (antes is null) return Respuesta.NotFound("El plan de estudios no existe.");
        if (!await manager.EstadoExiste(conexion, cod_estado)) return Respuesta.Failed<object>(mensaje: "El estado del plan no existe o está inactivo.");
        await manager.CambiarEstado(conexion, cod, cod_estado); await audit.Registrar(conexion, "aca_planes_estudio", cod.ToString(), "CAMBIAR_ESTADO", antes, new { cod_estado });
        return Respuesta.Success(await manager.Obtener(conexion, cod), "Estado actualizado.");
    }
    [HttpPut("{cod:int:min(1)}/Elementos"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> ReemplazarElementos(string conexion, int cod, [FromBody] List<PlanElementoRequest> items)
    {
        var antes = await manager.Obtener(conexion, cod); if (antes is null) return Respuesta.NotFound("El plan de estudios no existe.");
        try { await manager.ReemplazarElementos(conexion, cod, items); }
        catch (InvalidOperationException ex) { return Respuesta.Failed<object>(mensaje: ex.Message); }
        var despues = await manager.Obtener(conexion, cod); await audit.Registrar(conexion, "aca_plan_elementos", cod.ToString(), "REEMPLAZAR", antes.elementos, despues?.elementos);
        return Respuesta.Success(despues, "Malla del plan actualizada correctamente.");
    }
    [HttpPut("{cod:int:min(1)}/Requisitos"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> ReemplazarRequisitos(string conexion, int cod, [FromBody] List<PlanRequisitoRequest> items)
    {
        var antes = await manager.Obtener(conexion, cod); if (antes is null) return Respuesta.NotFound("El plan de estudios no existe.");
        try { await manager.ReemplazarRequisitos(conexion, cod, items); }
        catch (InvalidOperationException ex) { return Respuesta.Failed<object>(mensaje: ex.Message); }
        var despues = await manager.Obtener(conexion, cod); await audit.Registrar(conexion, "aca_plan_requisitos", cod.ToString(), "REEMPLAZAR", antes.requisitos, despues?.requisitos);
        return Respuesta.Success(despues, "Requisitos del plan actualizados correctamente.");
    }
}
