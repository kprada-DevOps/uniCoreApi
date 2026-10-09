using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniCore.Api.Helpers;
using UniCore.Api.Modules.PlanEstudios.Managers;
using UniCore.Api.Modules.PlanEstudios.Requests;
using UniCore.Api.Modules.Seguridad.Managers;

namespace UniCore.Api.Modules.PlanEstudios.Controllers;

[ApiController, Route("{conexion}/PlanEstudios/Asignaturas"), Authorize]
public sealed class AsignaturasController : ControllerBase
{
    private readonly AsignaturasManager _manager;
    private readonly AuditoriaManager _audit;

    public AsignaturasController(AsignaturasManager manager, AuditoriaManager audit)
    {
        _manager = manager;
        _audit = audit;
    }
    [HttpGet("ObtenerAsignaturas"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.CONSULTAR")]
    public async Task<ActionResult> Listar(string conexion, [FromQuery] bool incluirInactivas = false, [FromQuery] string? busqueda = null)
        => Respuesta.Success(await _manager.Listar(conexion, incluirInactivas, busqueda));
    [HttpGet("ObtenerAsignatura/{cod:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.CONSULTAR")]
    public async Task<ActionResult> Obtener(string conexion, int cod)
    {
        var item = await _manager.Obtener(conexion, cod); if (item is null) return Respuesta.NotFound("La asignatura no existe.");
        item.prerrequisitos = await _manager.Prerrequisitos(conexion, cod); return Respuesta.Success(item);
    }
    [HttpPost("CrearAsignatura"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> Crear(string conexion, [FromBody] AsignaturaRequest r)
    {
        r.codigo = r.codigo.Trim().ToUpperInvariant();
        if (await _manager.ExisteCodigo(conexion, r.codigo, null) is not null) return Respuesta.Conflict("El código de asignatura ya existe.");
        if (!await _manager.TipoExiste(conexion, r.cod_tipo_asignatura) || !await _manager.EstadoExiste(conexion, r.cod_estado)) return Respuesta.Failed<object>(mensaje: "El tipo o estado de asignatura no es válido.");
        var cod = await _manager.Crear(conexion, r); await _audit.Registrar(conexion, "aca_asignaturas", cod.ToString(), "CREAR", nuevo: r);
        return Respuesta.Success(await _manager.Obtener(conexion, cod), "Asignatura creada correctamente.");
    }
    [HttpPut("ActualizarAsignatura/{cod:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> Actualizar(string conexion, int cod, [FromBody] AsignaturaRequest r)
    {
        var antes = await _manager.Obtener(conexion, cod); if (antes is null) return Respuesta.NotFound("La asignatura no existe.");
        r.codigo = r.codigo.Trim().ToUpperInvariant();
        if (await _manager.ExisteCodigo(conexion, r.codigo, cod) is not null) return Respuesta.Conflict("El código de asignatura ya existe.");
        if (!await _manager.TipoExiste(conexion, r.cod_tipo_asignatura) || !await _manager.EstadoExiste(conexion, r.cod_estado)) return Respuesta.Failed<object>(mensaje: "El tipo o estado de asignatura no es válido.");
        await _manager.Actualizar(conexion, cod, r); await _audit.Registrar(conexion, "aca_asignaturas", cod.ToString(), "EDITAR", antes, r);
        return Respuesta.Success(await _manager.Obtener(conexion, cod), "Asignatura actualizada correctamente.");
    }
    [HttpPut("CambiarEstadoAsignatura/{cod:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> Estado(string conexion, int cod, [FromQuery] int cod_estado)
    {
        var antes = await _manager.Obtener(conexion, cod); if (antes is null) return Respuesta.NotFound("La asignatura no existe.");
        if (!await _manager.EstadoExiste(conexion, cod_estado)) return Respuesta.Failed<object>(mensaje: "El estado no existe o está inactivo.");
        await _manager.CambiarEstado(conexion, cod, cod_estado); await _audit.Registrar(conexion, "aca_asignaturas", cod.ToString(), "CAMBIAR_ESTADO", antes, new { cod_estado });
        return Respuesta.Success(await _manager.Obtener(conexion, cod), "Estado actualizado.");
    }
    [HttpGet("ObtenerPrerrequisitos/{cod:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.CONSULTAR")]
    public async Task<ActionResult> Prerrequisitos(string conexion, int cod)
        => await _manager.Obtener(conexion, cod) is null ? Respuesta.NotFound("La asignatura no existe.") : Respuesta.Success(await _manager.Prerrequisitos(conexion, cod));
    [HttpPost("AgregarPrerrequisito/{cod:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> AgregarPrerrequisito(string conexion, int cod, [FromBody] PrerrequisitoRequest r)
    {
        if (await _manager.Obtener(conexion, cod) is null) return Respuesta.NotFound("La asignatura no existe.");
        bool inserted;
        try { inserted = await _manager.AgregarPrerrequisito(conexion, cod, r); }
        catch (InvalidOperationException ex) { return Respuesta.Failed<object>(mensaje: ex.Message); }
        if (!inserted) return Respuesta.Conflict("La relación ya existe o alguna referencia no es válida.");
        await _audit.Registrar(conexion, "aca_asignatura_prerrequisitos", $"{cod}:{r.cod_asignatura_requisito}", "CREAR", nuevo: r);
        return Respuesta.Success(await _manager.Prerrequisitos(conexion, cod), "Prerrequisito agregado.");
    }
    [HttpDelete("EliminarPrerrequisito/{cod:int:min(1)}/{requisito:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> QuitarPrerrequisito(string conexion, int cod, int requisito)
    {
        if (!await _manager.QuitarPrerrequisito(conexion, cod, requisito)) return Respuesta.NotFound("La relación de prerrequisito no existe.");
        await _audit.Registrar(conexion, "aca_asignatura_prerrequisitos", $"{cod}:{requisito}", "ELIMINAR"); return Respuesta.Success(await _manager.Prerrequisitos(conexion, cod), "Prerrequisito eliminado.");
    }
}
