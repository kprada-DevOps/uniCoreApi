using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniCore.Api.Helpers;
using UniCore.Api.Modules.Seguridad.Managers;
using UniCore.Api.Modules.Seguridad.Requests;

namespace UniCore.Api.Modules.Seguridad.Controllers;

[ApiController, Route("{conexion}/[controller]"), Authorize(Policy="ROL:ADMIN")]
public sealed class PermisosController(PermisosManager manager, AuditoriaManager audit) : ControllerBase
{
    [HttpGet("ObtenerPermisos")]
    public async Task<ActionResult> Listar(string conexion, [FromQuery] int? cod_modulo=null, [FromQuery] bool incluirInactivos=false, [FromQuery] string? busqueda=null)
        => Respuesta.Success(await manager.Listar(conexion,cod_modulo,incluirInactivos,busqueda));
    [HttpGet("ObtenerPermiso/{cod:int:min(1)}")]
    public async Task<ActionResult> Obtener(string conexion,int cod) { var p=await manager.Obtener(cod,conexion); return p is null?Respuesta.NotFound("El permiso no existe."):Respuesta.Success(p); }
    [HttpPost("CrearPermiso")]
    public async Task<ActionResult> Crear(string conexion,[FromBody] PermisoRequest r)
    {
        r.codigo=r.codigo.Trim().ToUpperInvariant();
        if(!await manager.ModuloExiste(r.cod_modulo,conexion)) return Respuesta.Failed<object>(mensaje:"El módulo no existe.");
        if(await manager.ExisteCodigo(r.codigo,null,conexion) is not null) return Respuesta.Conflict("El código del permiso ya existe.");
        var id=await manager.Crear(r,conexion); await audit.Registrar(conexion,"seg_permisos",id.ToString(),"CREAR",nuevo:r);
        return Respuesta.Success(id,"Permiso creado correctamente");
    }
    [HttpPut("ActualizarPermiso/{cod:int:min(1)}")]
    public async Task<ActionResult> Actualizar(string conexion,int cod,[FromBody] PermisoRequest r)
    {
        r.codigo=r.codigo.Trim().ToUpperInvariant();
        if(!await manager.ModuloExiste(r.cod_modulo,conexion)) return Respuesta.Failed<object>(mensaje:"El módulo no existe.");
        if(await manager.ExisteCodigo(r.codigo,cod,conexion) is not null) return Respuesta.Conflict("El código del permiso ya existe.");
        var antes=await manager.Obtener(cod,conexion); if(antes is null) return Respuesta.NotFound("El permiso no existe.");
        await manager.Actualizar(cod,r,conexion); await audit.Registrar(conexion,"seg_permisos",cod.ToString(),"EDITAR",antes,r);
        return Respuesta.Success(true,"Permiso actualizado correctamente");
    }
    [HttpPut("CambiarEstado/{cod:int:min(1)}")]
    public async Task<ActionResult> Estado(string conexion,int cod,[FromQuery] bool activo)
    {
        var antes=await manager.Obtener(cod,conexion); if(antes is null) return Respuesta.NotFound("El permiso no existe.");
        await manager.Estado(cod,activo,conexion); await audit.Registrar(conexion,"seg_permisos",cod.ToString(),activo?"ACTIVAR":"DESACTIVAR",antes,new { activo });
        return Respuesta.Success(true,"Estado actualizado");
    }
}
