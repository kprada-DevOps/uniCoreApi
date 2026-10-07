using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniCore.Api.Helpers;
using UniCore.Api.Modules.Personas.Managers;

namespace UniCore.Api.Modules.Personas.Controllers;

/// <summary>
/// Catalogo de tipos de documento.
/// Solo lectura: es un catalogo sembrado por el esquema, y borrarlo dejaria personas
/// apuntando a un tipo inexistente.
/// </summary>
[ApiController]
[Route("{conexion}/[controller]")]
[Authorize]
public sealed class TipoDocumentoController : ControllerBase
{
    private readonly TipoDocumentoManager _tipoDocumentoManager;
    private readonly ILogger<TipoDocumentoController> _logger;

    public TipoDocumentoController(TipoDocumentoManager tipoDocumentoManager, ILogger<TipoDocumentoController> logger)
    {
        _tipoDocumentoManager = tipoDocumentoManager;
        _logger = logger;
    }

    /// <summary>
    /// Catalogo completo de tipos de documento activos.
    /// El nombre sigue la convencion de los demas listados del modulo (ObtenerPersonas,
    /// ObtenerEstudiantes), no el plural de la tabla per_tipos_documento.
    /// </summary>
    [HttpGet("ObtenerTipoDocumentos")]
    public async Task<ActionResult> ObtenerTipoDocumentos(string conexion)
    {
        var tipos = await _tipoDocumentoManager.ObtenerTiposDocumento(conexion);
        return Respuesta.Success(tipos);
    }

    [HttpGet("ObtenerTipoDocumento/{cod:int:min(1)}")]
    public async Task<ActionResult> ObtenerTipoDocumento(string conexion, int cod)
    {
        var tipo = await _tipoDocumentoManager.ObtenerTipoDocumento(cod, conexion);
        if (tipo is null)
            return Respuesta.NotFound($"No existe un tipo de documento con el código {cod}");

        return Respuesta.Success(tipo);
    }
}
