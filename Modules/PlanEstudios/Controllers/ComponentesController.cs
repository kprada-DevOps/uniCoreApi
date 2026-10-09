using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniCore.Api.Helpers;
using UniCore.Api.Modules.PlanEstudios.Managers;
using UniCore.Api.Modules.PlanEstudios.Requests;
using UniCore.Api.Modules.Seguridad.Managers;

namespace UniCore.Api.Modules.PlanEstudios.Controllers;

[ApiController, Route("{conexion}/PlanEstudios/Componentes"), Authorize]
public sealed class ComponentesController : ControllerBase
{
    private readonly ComponentesManager _manager;
    private readonly AuditoriaManager _audit;

    public ComponentesController(ComponentesManager manager, AuditoriaManager audit)
    {
        _manager = manager;
        _audit = audit;
    }
    [HttpGet("ObtenerComponentes"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.CONSULTAR")]
    public async Task<ActionResult> Listar(string conexion, [FromQuery] bool incluirInactivos = false, [FromQuery] string? busqueda = null)
        => Respuesta.Success(await _manager.Listar(conexion, incluirInactivos, busqueda));
    [HttpGet("ObtenerComponente/{cod:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.CONSULTAR")]
    public async Task<ActionResult> Obtener(string conexion, int cod)
    { var item = await _manager.Obtener(conexion, cod); return item is null ? Respuesta.NotFound("El componente no existe.") : Respuesta.Success(item); }
    [HttpPost("CrearComponente"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> Crear(string conexion, [FromBody] ComponenteRequest r)
    {
        r.codigo = r.codigo.Trim().ToUpperInvariant();
        if (await _manager.ExisteCodigo(conexion, r.codigo, null) is not null) return Respuesta.Conflict("El código de componente ya existe.");
        if (!await _manager.ReferenciasValidas(conexion, r)) return Respuesta.Failed<object>(mensaje: "El tipo, comportamiento o estado seleccionado no existe o está inactivo.");
        var cod = await _manager.Crear(conexion, r); await _audit.Registrar(conexion, "aca_componentes", cod.ToString(), "CREAR", nuevo: r);
        return Respuesta.Success(await _manager.Obtener(conexion, cod), "Componente creado correctamente.");
    }
    [HttpPut("ActualizarComponente/{cod:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> Actualizar(string conexion, int cod, [FromBody] ComponenteRequest r)
    {
        var antes = await _manager.Obtener(conexion, cod); if (antes is null) return Respuesta.NotFound("El componente no existe.");
        r.codigo = r.codigo.Trim().ToUpperInvariant();
        if (await _manager.ExisteCodigo(conexion, r.codigo, cod) is not null) return Respuesta.Conflict("El código de componente ya existe.");
        if (!await _manager.ReferenciasValidas(conexion, r)) return Respuesta.Failed<object>(mensaje: "El tipo, comportamiento o estado seleccionado no existe o está inactivo.");
        await _manager.Actualizar(conexion, cod, r); await _audit.Registrar(conexion, "aca_componentes", cod.ToString(), "EDITAR", antes, r);
        return Respuesta.Success(await _manager.Obtener(conexion, cod), "Componente actualizado correctamente.");
    }
    [HttpPut("CambiarEstadoComponente/{cod:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> Estado(string conexion, int cod, [FromQuery] int cod_estado)
    {
        var antes = await _manager.Obtener(conexion, cod); if (antes is null) return Respuesta.NotFound("El componente no existe.");
        if (!await _manager.EstadoExiste(conexion, cod_estado)) return Respuesta.Failed<object>(mensaje: "El estado no existe o está inactivo.");
        await _manager.CambiarEstado(conexion, cod, cod_estado); await _audit.Registrar(conexion, "aca_componentes", cod.ToString(), "CAMBIAR_ESTADO", antes, new { cod_estado });
        return Respuesta.Success(await _manager.Obtener(conexion, cod), "Estado actualizado.");
    }
    [HttpPut("GuardarOpcionesComponente/{cod:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> Opciones(string conexion, int cod, [FromBody] ComponenteOpcionesRequest r)
    {
        var antes = await _manager.Obtener(conexion, cod); if (antes is null) return Respuesta.NotFound("El componente no existe.");
        try { await _manager.ReemplazarOpciones(conexion, cod, r.opciones); } catch (InvalidOperationException ex) { return Respuesta.Failed<object>(mensaje: ex.Message); }
        var despues = await _manager.Obtener(conexion, cod); await _audit.Registrar(conexion, "aca_componente_opciones", cod.ToString(), "REEMPLAZAR", antes.opciones, despues?.opciones);
        return Respuesta.Success(despues, "Opciones del componente actualizadas.");
    }
}
