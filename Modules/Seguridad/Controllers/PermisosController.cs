using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniCore.Api.Helpers;
using UniCore.Api.Modules.Seguridad.Managers;
using UniCore.Api.Modules.Seguridad.Requests;

namespace UniCore.Api.Modules.Seguridad.Controllers;

[ApiController, Route("{conexion}/[controller]"), Authorize(Policy="PERMISO:SEGURIDAD.ADMINISTRAR")]
public sealed class PermisosController : ControllerBase
{
    private readonly PermisosManager _manager;
    private readonly AuditoriaManager _audit;

    public PermisosController(PermisosManager manager, AuditoriaManager audit)
    {
        _manager = manager;
        _audit = audit;
    }

    [HttpGet("ObtenerPermisos")]
    public async Task<ActionResult> Listar(string conexion, [FromQuery] int? cod_modulo=null, [FromQuery] bool incluirInactivos=false, [FromQuery] string? busqueda=null)
        => Respuesta.Success(await _manager.Listar(conexion,cod_modulo,incluirInactivos,busqueda));
    [HttpGet("ObtenerPermiso/{cod:int:min(1)}")]
    public async Task<ActionResult> Obtener(string conexion,int cod) { var p=await _manager.Obtener(cod,conexion); return p is null?Respuesta.NotFound("El permiso no existe."):Respuesta.Success(p); }
    [HttpPost("CrearPermiso")]
    public async Task<ActionResult> Crear(string conexion,[FromBody] PermisoRequest r)
    {
        r.codigo=r.codigo.Trim().ToUpperInvariant();
        if(!await _manager.ModuloExiste(r.cod_modulo,conexion)) return Respuesta.Failed<object>(mensaje:"El módulo no existe.");
        if(await _manager.ExisteCodigo(r.codigo,null,conexion) is not null) return Respuesta.Conflict("El código del permiso ya existe.");
        var id=await _manager.Crear(r,conexion); await _audit.Registrar(conexion,"seg_permisos",id.ToString(),"CREAR",nuevo:r);
        return Respuesta.Success(await _manager.Obtener(id,conexion),"Permiso creado correctamente");
    }
    [HttpPut("ActualizarPermiso/{cod:int:min(1)}")]
    public async Task<ActionResult> Actualizar(string conexion,int cod,[FromBody] PermisoRequest r)
    {
        r.codigo=r.codigo.Trim().ToUpperInvariant();
        if(!await _manager.ModuloExiste(r.cod_modulo,conexion)) return Respuesta.Failed<object>(mensaje:"El módulo no existe.");
        if(await _manager.ExisteCodigo(r.codigo,cod,conexion) is not null) return Respuesta.Conflict("El código del permiso ya existe.");
        var antes=await _manager.Obtener(cod,conexion); if(antes is null) return Respuesta.NotFound("El permiso no existe.");
        await _manager.Actualizar(cod,r,conexion); await _audit.Registrar(conexion,"seg_permisos",cod.ToString(),"EDITAR",antes,r);
        return Respuesta.Success(await _manager.Obtener(cod,conexion),"Permiso actualizado correctamente");
    }
    [HttpPut("CambiarEstado/{cod:int:min(1)}")]
    public async Task<ActionResult> Estado(string conexion,int cod,[FromQuery] bool activo)
    {
        var antes=await _manager.Obtener(cod,conexion); if(antes is null) return Respuesta.NotFound("El permiso no existe.");
        await _manager.Estado(cod,activo,conexion); await _audit.Registrar(conexion,"seg_permisos",cod.ToString(),activo?"ACTIVAR":"DESACTIVAR",antes,new { activo });
        return Respuesta.Success(await _manager.Obtener(cod,conexion),"Estado actualizado");
    }
}
