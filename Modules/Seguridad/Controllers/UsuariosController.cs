using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using UniCore.Api.Helpers;
using UniCore.Api.Modules.Seguridad.Managers;
using UniCore.Api.Modules.Seguridad.Requests;
using UniCore.Api.Modules.Auth.Managers;

namespace UniCore.Api.Modules.Seguridad.Controllers;

/// <summary>Administración inicial de cuentas del sistema.</summary>
[ApiController]
[Route("{conexion}/[controller]")]
[Authorize(Policy = "PERMISO:SEGURIDAD.ADMINISTRAR")]
public sealed class UsuariosController : ControllerBase
{
    private readonly UsuarioSeguridadManager _manager;
    private readonly ILogger<UsuariosController> _logger;
    private readonly AuditoriaManager _audit;
    private readonly AuthManager _auth;

    public UsuariosController(UsuarioSeguridadManager manager, ILogger<UsuariosController> logger, AuditoriaManager audit, AuthManager auth)
    {
        _manager = manager;
        _logger = logger;
        _audit = audit;
        _auth = auth;
    }

    [HttpGet("ObtenerUsuarios")]
    public async Task<ActionResult> ObtenerUsuarios(
        string conexion,
        [FromQuery] bool incluirInactivos = false,
        [FromQuery] string? busqueda = null,
        [FromQuery, Range(1, int.MaxValue)] int pagina = 1,
        [FromQuery, Range(1, 100)] int tamanoPagina = 20)
    {
        var usuarios = await _manager.ObtenerUsuarios(conexion, incluirInactivos, busqueda, pagina, tamanoPagina, await ActorEsSuperadmin(conexion));
        return Respuesta.Success(usuarios);
    }

    [HttpGet("ObtenerUsuario/{cod:long:min(1)}")]
    public async Task<ActionResult> ObtenerUsuario(string conexion, long cod)
    {
        if (await _manager.UsuarioEsSuperadmin(cod, conexion) && !await ActorEsSuperadmin(conexion))
            return Respuesta.NotFound($"No existe un usuario con el código {cod}");
        var usuario = await _manager.ObtenerUsuario(cod, conexion);
        return usuario is null
            ? Respuesta.NotFound($"No existe un usuario con el código {cod}")
            : Respuesta.Success(usuario);
    }

    [HttpGet("ObtenerRolesDisponibles")]
    public async Task<ActionResult> ObtenerRolesDisponibles(string conexion)
    {
        var esSuperadmin = long.TryParse(User.FindFirst("codusuario")?.Value, out var codUsuario)
            && await _auth.EsSuperadmin(codUsuario, conexion);
        return Respuesta.Success(await _manager.ObtenerRoles(conexion, esSuperadmin));
    }

    [HttpPost("CrearUsuario")]
    public async Task<ActionResult> CrearUsuario(string conexion, [FromBody] UsuarioCrearRequest request)
    {
        request.username = request.username.Trim();
        var relacionInvalida = await ValidarRelaciones(request.cod_persona, request.cod_roles, conexion);
        if (relacionInvalida is not null) return relacionInvalida;

        if (await _manager.UsernameExiste(request.username, null, conexion))
            return Respuesta.Conflict("El username ya está registrado.");

        var cod = await _manager.CrearUsuario(request, conexion);
        _logger.LogInformation("Usuario de seguridad creado con código {Cod} en {Conexion}", cod, conexion);
        await _audit.Registrar(conexion, "seg_usuarios", cod.ToString(), "CREAR", nuevo: new { request.cod_persona, request.username, request.activo, request.cod_roles });
        var creado = await _manager.ObtenerUsuario(cod, conexion);
        return Respuesta.Success(creado, "Usuario creado correctamente");
    }

    [HttpPut("ActualizarUsuario/{cod:long:min(1)}")]
    public async Task<ActionResult> ActualizarUsuario(
        string conexion,
        long cod,
        [FromBody] UsuarioActualizarRequest request)
    {
        request.username = request.username.Trim();
        if (await _manager.UsuarioEsSuperadmin(cod, conexion) && !await ActorEsSuperadmin(conexion))
            return Respuesta.Failed<object>(mensaje: "Solo un superadmin puede modificar esta cuenta.");
        if (await _manager.UsernameExiste(request.username, cod, conexion))
            return Respuesta.Conflict("El username ya está registrado.");

        var relacionInvalida = await ValidarRelaciones(request.cod_persona, request.cod_roles, conexion, cod);
        if (relacionInvalida is not null) return relacionInvalida;

        var anterior = await _manager.ObtenerUsuario(cod, conexion);
        var actualizado = await _manager.ActualizarUsuario(cod, request, conexion);
        if (actualizado) await _audit.Registrar(conexion, "seg_usuarios", cod.ToString(), "EDITAR", anterior, new { request.cod_persona, request.username, request.activo, request.cod_roles });
        if (!actualizado)
            return Respuesta.NotFound($"No existe un usuario con el código {cod}");

        var usuarioActualizado = await _manager.ObtenerUsuario(cod, conexion);
        return Respuesta.Success(usuarioActualizado, "Usuario actualizado correctamente");
    }

    [HttpPut("ActivarUsuario/{cod:long:min(1)}")]
    public async Task<ActionResult> ActivarUsuario(string conexion, long cod)
        => await EstablecerEstado(cod, true, conexion);

    [HttpPut("DesactivarUsuario/{cod:long:min(1)}")]
    public async Task<ActionResult> DesactivarUsuario(string conexion, long cod)
        => await EstablecerEstado(cod, false, conexion);

    [HttpPut("CambiarContrasena/{cod:long:min(1)}")]
    public async Task<ActionResult> CambiarContrasena(
        string conexion,
        long cod,
        [FromBody] CambiarContrasenaRequest request)
    {
        if (await _manager.UsuarioEsSuperadmin(cod, conexion) && !await ActorEsSuperadmin(conexion))
            return Respuesta.Failed<object>(mensaje: "Solo un superadmin puede cambiar la contraseña de esta cuenta.");
        var actualizado = await _manager.CambiarContrasena(cod, request.password, conexion);
        if (actualizado) await _audit.Registrar(conexion, "seg_usuarios", cod.ToString(), "CAMBIAR_CONTRASENA");
        return actualizado
            ? Respuesta.Success(true, "Contraseña actualizada correctamente")
            : Respuesta.NotFound($"No existe un usuario con el código {cod}");
    }

    private async Task<ActionResult> EstablecerEstado(long cod, bool activo, string conexion)
    {
        if (await _manager.UsuarioEsSuperadmin(cod, conexion) && !await ActorEsSuperadmin(conexion))
            return Respuesta.Failed<object>(mensaje: "Solo un superadmin puede cambiar el estado de esta cuenta.");
        var actualizado = await _manager.EstablecerActivo(cod, activo, conexion);
        if (!actualizado)
            return Respuesta.NotFound($"No existe un usuario con el código {cod}");

        await _audit.Registrar(conexion, "seg_usuarios", cod.ToString(), activo ? "ACTIVAR" : "DESACTIVAR", nuevo: new { activo });
        var mensaje = activo ? "Usuario activado correctamente" : "Usuario desactivado correctamente";
        var usuario = await _manager.ObtenerUsuario(cod, conexion);
        return Respuesta.Success(usuario?.usuario, mensaje);
    }

    private Task<bool> ActorEsSuperadmin(string conexion)
        => long.TryParse(User.FindFirst("codusuario")?.Value, out var codUsuario)
            ? _auth.EsSuperadmin(codUsuario, conexion)
            : Task.FromResult(false);

    private async Task<ActionResult?> ValidarRelaciones(
        long? codPersona,
        IEnumerable<int> codRoles,
        string conexion,
        long? codUsuario = null)
    {
        if (!await _manager.PersonaExiste(codPersona, conexion))
            return Respuesta.Failed<object>(mensaje: "La persona asociada no existe.");

        var rolesInvalidos = await _manager.RolesInvalidos(codRoles, conexion, codUsuario);
        if (rolesInvalidos.Count > 0)
            return Respuesta.Failed<object>(mensaje: $"Los roles no existen o no están activos: {string.Join(", ", rolesInvalidos)}.");

        var tieneRolSuperadmin = await ActorEsSuperadmin(conexion);
        var rolesSuperadmin = await _manager.RolesSuperadmin(codRoles, conexion);
        if (!tieneRolSuperadmin && rolesSuperadmin.Count > 0)
            return Respuesta.Failed<object>(mensaje: "Solo un superadmin puede asignar el rol superadmin.");

        return null;
    }
}
