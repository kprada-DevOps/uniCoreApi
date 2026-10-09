using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniCore.Api.Helpers;
using UniCore.Api.Modules.Seguridad.Managers;
using UniCore.Api.Modules.Seguridad.Requests;

namespace UniCore.Api.Modules.Seguridad.Controllers;

[ApiController, Route("{conexion}/[controller]")]
public sealed class MenuController : ControllerBase
{
    private readonly MenuManager _manager;
    private readonly AuditoriaManager _audit;

    public MenuController(MenuManager manager, AuditoriaManager audit)
    {
        _manager = manager;
        _audit = audit;
    }

    [Authorize]
    [HttpGet("ObtenerMenuUsuario")]
    public async Task<ActionResult> ParaUsuario(string conexion)
    {
        if(!long.TryParse(User.FindFirst("codusuario")?.Value,out var usuario)) return Unauthorized();
        return Respuesta.Success(await _manager.ParaUsuario(usuario,conexion));
    }
    [Authorize(Policy="PERMISO:SEGURIDAD.ADMINISTRAR")]
    [HttpGet("ObtenerMenu")]
    public async Task<ActionResult> Listar(string conexion,[FromQuery] int? cod_modulo=null,[FromQuery] bool incluirInactivos=false)=>Respuesta.Success(await _manager.Listar(conexion,cod_modulo,incluirInactivos));
    [Authorize(Policy="PERMISO:SEGURIDAD.ADMINISTRAR")]
    [HttpGet("ObtenerMenu/{cod:int:min(1)}")]
    public async Task<ActionResult> Obtener(string conexion,int cod){var item=await _manager.Obtener(cod,conexion);return item is null?Respuesta.NotFound("El elemento de menú no existe."):Respuesta.Success(item);}
    [Authorize(Policy="PERMISO:SEGURIDAD.ADMINISTRAR")]
    [HttpPost("CrearMenu")]
    public async Task<ActionResult> Crear(string conexion,[FromBody] MenuRequest r)
    {
        r.codigo=r.codigo.Trim().ToUpperInvariant();
        r.ruta=MenuManager.NormalizarRuta(r.ruta);
        if(!await _manager.ModuloExiste(r.cod_modulo,conexion))return Respuesta.Failed<object>(mensaje:"El módulo no existe o está inactivo.");
        if(!await _manager.PadreValido(r.cod_padre,r.cod_modulo,null,conexion))return Respuesta.Failed<object>(mensaje:"El padre debe estar activo; si pertenece a otro módulo debe ser de tipo GRUPO.");
        if(await _manager.CodigoExiste(r.codigo,null,conexion)is not null)return Respuesta.Conflict("El código del menú ya existe.");
        var id=await _manager.Crear(r,conexion);await _audit.Registrar(conexion,"seg_menu",id.ToString(),"CREAR",nuevo:r);return Respuesta.Success(await _manager.Obtener(id,conexion),"Elemento de menú creado");
    }
    [Authorize(Policy="PERMISO:SEGURIDAD.ADMINISTRAR")]
    [HttpPut("ActualizarMenu/{cod:int:min(1)}")]
    public async Task<ActionResult> Actualizar(string conexion,int cod,[FromBody] MenuRequest r)
    {
        r.codigo=r.codigo.Trim().ToUpperInvariant();
        r.ruta=MenuManager.NormalizarRuta(r.ruta);
        if(!await _manager.ModuloExiste(r.cod_modulo,conexion))return Respuesta.Failed<object>(mensaje:"El módulo no existe o está inactivo.");
        if(!await _manager.PadreValido(r.cod_padre,r.cod_modulo,cod,conexion))return Respuesta.Failed<object>(mensaje:"El padre debe estar activo, no formar un ciclo y ser de tipo GRUPO si pertenece a otro módulo.");
        if(await _manager.CodigoExiste(r.codigo,cod,conexion)is not null)return Respuesta.Conflict("El código del menú ya existe.");
        var antes=await _manager.Listar(conexion,r.cod_modulo,true);var anterior=antes.FirstOrDefault(x=>x.cod==cod);if(anterior is null)return Respuesta.NotFound("El elemento de menú no existe.");
        await _manager.Actualizar(cod,r,conexion);await _audit.Registrar(conexion,"seg_menu",cod.ToString(),"EDITAR",anterior,r);return Respuesta.Success(await _manager.Obtener(cod,conexion),"Elemento de menú actualizado");
    }
    [Authorize(Policy="PERMISO:SEGURIDAD.ADMINISTRAR")]
    [HttpPut("CambiarEstado/{cod:int:min(1)}")]
    public async Task<ActionResult> Estado(string conexion,int cod,[FromQuery] bool activo)
    {
        var lista=await _manager.Listar(conexion,null,true);var antes=lista.FirstOrDefault(x=>x.cod==cod);if(antes is null)return Respuesta.NotFound("El elemento de menú no existe.");
        await _manager.Estado(cod,activo,conexion);await _audit.Registrar(conexion,"seg_menu",cod.ToString(),activo?"ACTIVAR":"DESACTIVAR",antes,new{activo});return Respuesta.Success(await _manager.Obtener(cod,conexion),"Estado actualizado");
    }
}
