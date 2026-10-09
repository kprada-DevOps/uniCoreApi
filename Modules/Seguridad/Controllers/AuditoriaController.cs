using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniCore.Api.Helpers;
using UniCore.Api.Modules.Seguridad.Managers;

namespace UniCore.Api.Modules.Seguridad.Controllers;

[ApiController, Route("{conexion}/[controller]"), Authorize(Policy="PERMISO:SEGURIDAD.ADMINISTRAR")]
public sealed class AuditoriaController : ControllerBase
{
    private readonly AuditoriaManager _manager;

    public AuditoriaController(AuditoriaManager manager)
    {
        _manager = manager;
    }

    [HttpGet("ObtenerAuditoria")]
    public async Task<ActionResult> Listar(string conexion,[FromQuery] int pagina=1,[FromQuery] int tamanoPagina=20,[FromQuery] long? cod_usuario=null,[FromQuery] string? tabla=null,[FromQuery] DateTime? desde=null,[FromQuery] DateTime? hasta=null)
        => Respuesta.Success(await _manager.Listar(conexion,pagina,tamanoPagina,cod_usuario,tabla,desde,hasta));
}
