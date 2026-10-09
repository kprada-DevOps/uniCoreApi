using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniCore.Api.Helpers;
using UniCore.Api.Modules.Seguridad.Managers;
using UniCore.Api.Modules.Seguridad.Requests;

namespace UniCore.Api.Modules.Seguridad.Controllers;

[ApiController, Route("{conexion}/[controller]"), Authorize(Policy="PERMISO:SEGURIDAD.ADMINISTRAR")]
public sealed class ModulosController : ControllerBase
{
    private readonly ModulosManager _manager;
    private readonly AuditoriaManager _audit;

    public ModulosController(ModulosManager manager, AuditoriaManager audit)
    {
        _manager = manager;
        _audit = audit;
    }

    [HttpGet("ObtenerModulos")]
    public async Task<ActionResult> Listar(string conexion,[FromQuery] bool incluirInactivos=false)=>Respuesta.Success(await _manager.Listar(conexion,incluirInactivos));
    [HttpGet("ObtenerModulo/{cod:int:min(1)}")]
    public async Task<ActionResult> Obtener(string conexion,int cod){var m=await _manager.Obtener(cod,conexion);return m is null?Respuesta.NotFound("El módulo no existe."):Respuesta.Success(m);}
    [HttpPost("CrearModulo")]
    public async Task<ActionResult> Crear(string conexion,[FromBody] ModuloRequest r)
    {
        r.codigo=r.codigo.Trim().ToUpperInvariant(); if(r.fecha_inicio.HasValue&&r.fecha_fin.HasValue&&r.fecha_fin<r.fecha_inicio)return Respuesta.Failed<object>(mensaje:"La fecha fin debe ser igual o posterior a la fecha inicio.");
        if(await _manager.CodigoExiste(r.codigo,null,conexion)is not null)return Respuesta.Conflict("El código del módulo ya existe.");
        var id=await _manager.Crear(r,conexion); await _audit.Registrar(conexion,"seg_modulos",id.ToString(),"CREAR",nuevo:r); return Respuesta.Success(await _manager.Obtener(id,conexion),"Módulo creado correctamente");
    }
    [HttpPut("ActualizarModulo/{cod:int:min(1)}")]
    public async Task<ActionResult> Actualizar(string conexion,int cod,[FromBody] ModuloRequest r)
    {
        r.codigo=r.codigo.Trim().ToUpperInvariant(); if(r.fecha_inicio.HasValue&&r.fecha_fin.HasValue&&r.fecha_fin<r.fecha_inicio)return Respuesta.Failed<object>(mensaje:"La fecha fin debe ser igual o posterior a la fecha inicio.");
        if(await _manager.CodigoExiste(r.codigo,cod,conexion)is not null)return Respuesta.Conflict("El código del módulo ya existe.");
        var antes=await _manager.Obtener(cod,conexion);if(antes is null)return Respuesta.NotFound("El módulo no existe.");
        await _manager.Actualizar(cod,r,conexion);await _audit.Registrar(conexion,"seg_modulos",cod.ToString(),"EDITAR",antes,r);return Respuesta.Success(await _manager.Obtener(cod,conexion),"Módulo actualizado correctamente");
    }
    [HttpPut("CambiarEstado/{cod:int:min(1)}")]
    public async Task<ActionResult> Estado(string conexion,int cod,[FromQuery] bool activo)
    {
        var antes=await _manager.Obtener(cod,conexion);if(antes is null)return Respuesta.NotFound("El módulo no existe.");
        await _manager.Estado(cod,activo,conexion);await _audit.Registrar(conexion,"seg_modulos",cod.ToString(),activo?"ACTIVAR":"DESACTIVAR",antes,new{activo});return Respuesta.Success(await _manager.Obtener(cod,conexion),"Estado actualizado");
    }
}
