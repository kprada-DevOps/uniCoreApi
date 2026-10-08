using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniCore.Api.Helpers;
using UniCore.Api.Modules.Seguridad.Managers;
using UniCore.Api.Modules.Seguridad.Requests;

namespace UniCore.Api.Modules.Seguridad.Controllers;

[ApiController]
[Route("{conexion}/[controller]")]
[Authorize(Policy = "PERMISO:SEGURIDAD.ADMINISTRAR")]
public sealed class RolesController : ControllerBase
{
    private readonly RolesManager _manager;
    private readonly AuditoriaManager _audit;
    public RolesController(RolesManager manager, AuditoriaManager audit) { _manager = manager; _audit = audit; }

    [HttpGet("ObtenerRoles")]
    public async Task<ActionResult> ObtenerRoles(string conexion, [FromQuery] bool incluirInactivos = false, [FromQuery] string? busqueda = null)
        => Respuesta.Success(await _manager.ObtenerRoles(conexion, incluirInactivos, busqueda));

    [HttpGet("ObtenerRol/{cod:int:min(1)}")]
    public async Task<ActionResult> ObtenerRol(string conexion, int cod)
    {
        if (await _manager.EsRolSuperadmin(cod, conexion)) return Respuesta.NotFound("El rol no existe.");
        var rol = await _manager.ObtenerRol(cod, conexion);
        return rol is null ? Respuesta.NotFound("El rol no existe.") : Respuesta.Success(rol);
    }

    [HttpPost("CrearRol")]
    public async Task<ActionResult> CrearRol(string conexion, [FromBody] RolRequest request)
    {
        request.codigo = request.codigo.Trim().ToUpperInvariant();
        if (await _manager.CodigoExiste(request.codigo, null, conexion)) return Respuesta.Conflict("El código de rol ya existe.");
        var invalidos = await _manager.PermisosInvalidos(request.cod_permisos, conexion);
        if (invalidos.Count > 0) return Respuesta.Failed<object>(mensaje: $"Permisos inexistentes o inactivos: {string.Join(", ", invalidos)}.");
        var cod = await _manager.Crear(request, conexion);
        await _audit.Registrar(conexion, "seg_roles", cod.ToString(), "CREAR", nuevo: new { request.codigo, request.nombre, request.descripcion, request.activo, request.cod_permisos });
        var creado = await _manager.ObtenerRol(cod, conexion);
        return Respuesta.Success(creado?.rol, "Rol creado correctamente");
    }

    [HttpPut("ActualizarRol/{cod:int:min(1)}")]
    public async Task<ActionResult> ActualizarRol(string conexion, int cod, [FromBody] RolRequest request)
    {
        request.codigo = request.codigo.Trim().ToUpperInvariant();
        if (await _manager.EsRolSuperadmin(cod, conexion)) return Respuesta.Failed<object>(mensaje: "El rol superadmin solo puede administrarse mediante un procedimiento controlado.");
        if (await _manager.CodigoExiste(request.codigo, cod, conexion)) return Respuesta.Conflict("El código de rol ya existe.");
        var invalidos = await _manager.PermisosInvalidos(request.cod_permisos, conexion);
        if (invalidos.Count > 0) return Respuesta.Failed<object>(mensaje: $"Permisos inexistentes o inactivos: {string.Join(", ", invalidos)}.");
        var anterior = await _manager.ObtenerRol(cod, conexion);
        if (anterior is null) return Respuesta.NotFound("El rol no existe.");
        await _manager.Actualizar(cod, request, conexion);
        await _audit.Registrar(conexion, "seg_roles", cod.ToString(), "EDITAR", anterior, new { request.codigo, request.nombre, request.descripcion, request.activo, request.cod_permisos });
        var actualizado = await _manager.ObtenerRol(cod, conexion);
        return Respuesta.Success(actualizado?.rol, "Rol actualizado correctamente");
    }

    [HttpPut("ActivarRol/{cod:int:min(1)}")]
    public async Task<ActionResult> ActivarRol(string conexion, int cod)
        => await EstablecerActivo(cod, true, conexion);

    [HttpPut("DesactivarRol/{cod:int:min(1)}")]
    public async Task<ActionResult> DesactivarRol(string conexion, int cod)
        => await EstablecerActivo(cod, false, conexion);

    private async Task<ActionResult> EstablecerActivo(int cod, bool activo, string conexion)
    {
        if (await _manager.EsRolSuperadmin(cod, conexion)) return Respuesta.Failed<object>(mensaje: "El rol superadmin no se puede desactivar desde esta pantalla.");
        var antes = await _manager.ObtenerRol(cod, conexion);
        if (antes is null) return Respuesta.NotFound("El rol no existe.");
        await _manager.EstablecerActivo(cod, activo, conexion);
        await _audit.Registrar(conexion, "seg_roles", cod.ToString(), activo ? "ACTIVAR" : "DESACTIVAR", antes, new { activo });
        var actualizado = await _manager.ObtenerRol(cod, conexion);
        return Respuesta.Success(actualizado?.rol, activo ? "Rol activado correctamente" : "Rol desactivado correctamente");
    }
}
