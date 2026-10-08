using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniCore.Api.Helpers;
using UniCore.Api.Modules.PlanEstudios.Managers;
using UniCore.Api.Modules.PlanEstudios.Requests;
using UniCore.Api.Modules.Seguridad.Managers;

namespace UniCore.Api.Modules.PlanEstudios.Controllers;

[ApiController, Route("{conexion}/PlanEstudios/Asignaturas"), Authorize]
public sealed class AsignaturasController(AsignaturasManager manager, AuditoriaManager audit) : ControllerBase
{
    [HttpGet, Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.CONSULTAR")]
    public async Task<ActionResult> Listar(string conexion, [FromQuery] bool incluirInactivas = false, [FromQuery] string? busqueda = null)
        => Respuesta.Success(await manager.Listar(conexion, incluirInactivas, busqueda));
    [HttpGet("{cod:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.CONSULTAR")]
    public async Task<ActionResult> Obtener(string conexion, int cod)
    {
        var item = await manager.Obtener(conexion, cod); if (item is null) return Respuesta.NotFound("La asignatura no existe.");
        item.prerrequisitos = await manager.Prerrequisitos(conexion, cod); return Respuesta.Success(item);
    }
    [HttpPost, Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> Crear(string conexion, [FromBody] AsignaturaRequest r)
    {
        r.codigo = r.codigo.Trim().ToUpperInvariant();
        if (await manager.ExisteCodigo(conexion, r.codigo, null) is not null) return Respuesta.Conflict("El código de asignatura ya existe.");
        if (!await manager.TipoExiste(conexion, r.cod_tipo_asignatura) || !await manager.EstadoExiste(conexion, r.cod_estado)) return Respuesta.Failed<object>(mensaje: "El tipo o estado de asignatura no es válido.");
        var cod = await manager.Crear(conexion, r); await audit.Registrar(conexion, "aca_asignaturas", cod.ToString(), "CREAR", nuevo: r);
        return Respuesta.Success(await manager.Obtener(conexion, cod), "Asignatura creada correctamente.");
    }
    [HttpPut("{cod:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> Actualizar(string conexion, int cod, [FromBody] AsignaturaRequest r)
    {
        var antes = await manager.Obtener(conexion, cod); if (antes is null) return Respuesta.NotFound("La asignatura no existe.");
        r.codigo = r.codigo.Trim().ToUpperInvariant();
        if (await manager.ExisteCodigo(conexion, r.codigo, cod) is not null) return Respuesta.Conflict("El código de asignatura ya existe.");
        if (!await manager.TipoExiste(conexion, r.cod_tipo_asignatura) || !await manager.EstadoExiste(conexion, r.cod_estado)) return Respuesta.Failed<object>(mensaje: "El tipo o estado de asignatura no es válido.");
        await manager.Actualizar(conexion, cod, r); await audit.Registrar(conexion, "aca_asignaturas", cod.ToString(), "EDITAR", antes, r);
        return Respuesta.Success(await manager.Obtener(conexion, cod), "Asignatura actualizada correctamente.");
    }
    [HttpPut("{cod:int:min(1)}/Estado"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> Estado(string conexion, int cod, [FromQuery] int cod_estado)
    {
        var antes = await manager.Obtener(conexion, cod); if (antes is null) return Respuesta.NotFound("La asignatura no existe.");
        if (!await manager.EstadoExiste(conexion, cod_estado)) return Respuesta.Failed<object>(mensaje: "El estado no existe o está inactivo.");
        await manager.CambiarEstado(conexion, cod, cod_estado); await audit.Registrar(conexion, "aca_asignaturas", cod.ToString(), "CAMBIAR_ESTADO", antes, new { cod_estado });
        return Respuesta.Success(await manager.Obtener(conexion, cod), "Estado actualizado.");
    }
    [HttpGet("{cod:int:min(1)}/Prerrequisitos"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.CONSULTAR")]
    public async Task<ActionResult> Prerrequisitos(string conexion, int cod)
        => await manager.Obtener(conexion, cod) is null ? Respuesta.NotFound("La asignatura no existe.") : Respuesta.Success(await manager.Prerrequisitos(conexion, cod));
    [HttpPost("{cod:int:min(1)}/Prerrequisitos"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> AgregarPrerrequisito(string conexion, int cod, [FromBody] PrerrequisitoRequest r)
    {
        if (await manager.Obtener(conexion, cod) is null) return Respuesta.NotFound("La asignatura no existe.");
        bool inserted;
        try { inserted = await manager.AgregarPrerrequisito(conexion, cod, r); }
        catch (InvalidOperationException ex) { return Respuesta.Failed<object>(mensaje: ex.Message); }
        if (!inserted) return Respuesta.Conflict("La relación ya existe o alguna referencia no es válida.");
        await audit.Registrar(conexion, "aca_asignatura_prerrequisitos", $"{cod}:{r.cod_asignatura_requisito}", "CREAR", nuevo: r);
        return Respuesta.Success(await manager.Prerrequisitos(conexion, cod), "Prerrequisito agregado.");
    }
    [HttpDelete("{cod:int:min(1)}/Prerrequisitos/{requisito:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> QuitarPrerrequisito(string conexion, int cod, int requisito)
    {
        if (!await manager.QuitarPrerrequisito(conexion, cod, requisito)) return Respuesta.NotFound("La relación de prerrequisito no existe.");
        await audit.Registrar(conexion, "aca_asignatura_prerrequisitos", $"{cod}:{requisito}", "ELIMINAR"); return Respuesta.Success(await manager.Prerrequisitos(conexion, cod), "Prerrequisito eliminado.");
    }
}
