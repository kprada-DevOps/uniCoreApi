using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniCore.Api.Helpers;
using UniCore.Api.Modules.Idiomas.Dto;
using UniCore.Api.Modules.Idiomas.Managers;
using UniCore.Api.Modules.Idiomas.Requests;
using UniCore.Api.Modules.Seguridad.Managers;

namespace UniCore.Api.Modules.Idiomas.Controllers;

[ApiController, Route("{conexion}/Idiomas"), Authorize]
public sealed class IdiomasController : ControllerBase
{
    private const string Consultar = "PERMISO:IDIOMAS.CONSULTAR";
    private const string Administrar = "PERMISO:IDIOMAS.ADMINISTRAR";

    private readonly IdiomasManager _manager;
    private readonly AuditoriaManager _audit;

    public IdiomasController(IdiomasManager manager, AuditoriaManager audit)
    {
        _manager = manager;
        _audit = audit;
    }

    [HttpGet("ObtenerEstadosConfiguracion"), Authorize(Policy = Consultar)]
    public async Task<ActionResult> EstadosConfiguracion(string conexion) =>
        Respuesta.Success(await _manager.EstadosConfiguracion(conexion));

    [HttpGet("ObtenerAsignaturas"), Authorize(Policy = Consultar)]
    public async Task<ActionResult> Asignaturas(string conexion) =>
        Respuesta.Success(await _manager.Asignaturas(conexion));

    [HttpGet("ObtenerIdiomas"), Authorize(Policy = Consultar)]
    public async Task<ActionResult> Idiomas(string conexion, [FromQuery] bool incluirInactivas = false) =>
        Respuesta.Success(await _manager.Idiomas(conexion, incluirInactivas));

    [HttpPost("CrearIdioma"), Authorize(Policy = Administrar)]
    public async Task<ActionResult> CrearIdioma(string conexion, [FromBody] IdiomaRequest request)
    {
        request.codigo = request.codigo.Trim().ToUpperInvariant();
        if (await _manager.ExisteCodigo("idiomas", request.codigo, null, null, conexion) is not null)
            return Respuesta.Conflict("El código del idioma ya existe.");
        if (!await _manager.ReferenciasValidas(request, conexion))
            return Respuesta.Failed<object>(mensaje: "El estado no existe o está inactivo.");
        var cod = await _manager.CrearIdioma(conexion, request);
        await _audit.Registrar(conexion, "idi_idiomas", cod.ToString(), "CREAR", nuevo: request);
        var item = (await _manager.Idiomas(conexion, true)).FirstOrDefault(idioma => idioma.cod == cod);
        return Respuesta.Success(item, "Idioma creado.");
    }

    [HttpPut("ActualizarIdioma/{cod:int:min(1)}"), Authorize(Policy = Administrar)]
    public async Task<ActionResult> ActualizarIdioma(string conexion, int cod, [FromBody] IdiomaRequest request)
    {
        if (!await _manager.Existe("idiomas", cod, conexion))
            return Respuesta.NotFound("El idioma no existe.");
        request.codigo = request.codigo.Trim().ToUpperInvariant();
        if (await _manager.ExisteCodigo("idiomas", request.codigo, null, cod, conexion) is not null)
            return Respuesta.Conflict("El código del idioma ya existe.");
        if (!await _manager.ReferenciasValidas(request, conexion))
            return Respuesta.Failed<object>(mensaje: "El estado no existe o está inactivo.");
        await _manager.ActualizarIdioma(conexion, cod, request);
        await _audit.Registrar(conexion, "idi_idiomas", cod.ToString(), "EDITAR", nuevo: request);
        return Respuesta.Success(await _manager.Idiomas(conexion, true), "Idioma actualizado.");
    }

    [HttpGet("ObtenerNivelesIdioma"), Authorize(Policy = Consultar)]
    public async Task<ActionResult> Niveles(string conexion, [FromQuery] bool incluirInactivos = false) =>
        Respuesta.Success(await _manager.Niveles(conexion, incluirInactivos));

    [HttpGet("ObtenerFormatoNivel"), Authorize(Policy = Consultar)]
    public async Task<ActionResult> FormatoNivel(string conexion) =>
        Respuesta.Success(await _manager.FormatoNivel(conexion));

    [HttpPut("ActualizarFormatoNivel"), Authorize(Policy = Administrar)]
    public async Task<ActionResult> ActualizarFormatoNivel(string conexion, [FromBody] FormatoNivelIdiomaRequest request)
    {
        var formato = request.formato_nivel.Trim().ToUpperInvariant();
        if (formato is not ("ROMANO" or "ARABIGO"))
            return Respuesta.Failed<object>(mensaje: "El formato debe ser ROMANO o ARABIGO.");
        await _manager.ActualizarFormatoNivel(conexion, formato);
        await _audit.Registrar(conexion, "idi_configuracion", "1", "EDITAR", nuevo: new { formato_nivel = formato });
        return Respuesta.Success(await _manager.FormatoNivel(conexion), "Formato de niveles actualizado.");
    }

    [HttpPost("CrearNivelIdioma"), Authorize(Policy = Administrar)]
    public async Task<ActionResult> CrearNivel(string conexion, [FromBody] NivelIdiomaRequest request)
    {
        if (!await _manager.ReferenciasValidas(request, conexion))
            return Respuesta.Failed<object>(mensaje: "El estado no existe o está inactivo.");
        var cod = await _manager.CrearNivel(conexion, request);
        await _audit.Registrar(conexion, "idi_niveles_idioma", cod.ToString(), "CREAR", nuevo: request);
        return Respuesta.Success(await _manager.Niveles(conexion, true), "Nivel de idioma creado.");
    }

    [HttpPut("ActualizarNivelIdioma/{cod:int:min(1)}"), Authorize(Policy = Administrar)]
    public async Task<ActionResult> ActualizarNivel(string conexion, int cod, [FromBody] NivelIdiomaRequest request)
    {
        if (!await _manager.Existe("niveles", cod, conexion))
            return Respuesta.NotFound("El nivel no existe.");
        if (!await _manager.ReferenciasValidas(request, conexion))
            return Respuesta.Failed<object>(mensaje: "El estado no existe o está inactivo.");
        await _manager.ActualizarNivel(conexion, cod, request);
        await _audit.Registrar(conexion, "idi_niveles_idioma", cod.ToString(), "EDITAR", nuevo: request);
        return Respuesta.Success(await _manager.Niveles(conexion, true), "Nivel de idioma actualizado.");
    }

    [HttpGet("ObtenerRutasIdioma"), Authorize(Policy = Consultar)]
    public async Task<ActionResult> Rutas(string conexion, [FromQuery] int? cod_idioma = null, [FromQuery] bool incluirInactivas = false) =>
        Respuesta.Success(await _manager.Rutas(conexion, cod_idioma, incluirInactivas));

    [HttpPost("CrearRutaIdioma"), Authorize(Policy = Administrar)]
    public async Task<ActionResult> CrearRuta(string conexion, [FromBody] RutaIdiomaRequest request)
    {
        request.codigo = request.codigo.Trim().ToUpperInvariant();
        if (await _manager.ExisteCodigo("rutas", request.codigo, request.cod_idioma, null, conexion) is not null)
            return Respuesta.Conflict("El código ya existe para este idioma.");
        if (!await _manager.ReferenciasValidas(request, conexion))
            return Respuesta.Failed<object>(mensaje: "El idioma, nivel final o estado no es válido.");
        var cod = await _manager.CrearRuta(conexion, request);
        await _audit.Registrar(conexion, "idi_rutas_idioma", cod.ToString(), "CREAR", nuevo: request);
        return Respuesta.Success(await _manager.Rutas(conexion, request.cod_idioma, true), "Ruta creada.");
    }

    [HttpPut("ActualizarRutaIdioma/{cod:int:min(1)}"), Authorize(Policy = Administrar)]
    public async Task<ActionResult> ActualizarRuta(string conexion, int cod, [FromBody] RutaIdiomaRequest request)
    {
        if (!await _manager.Existe("rutas", cod, conexion))
            return Respuesta.NotFound("La ruta no existe.");
        request.codigo = request.codigo.Trim().ToUpperInvariant();
        if (await _manager.ExisteCodigo("rutas", request.codigo, request.cod_idioma, cod, conexion) is not null)
            return Respuesta.Conflict("El código ya existe para este idioma.");
        if (!await _manager.ReferenciasValidas(request, conexion))
            return Respuesta.Failed<object>(mensaje: "El idioma, nivel final o estado no es válido.");
        await _manager.ActualizarRuta(conexion, cod, request);
        await _audit.Registrar(conexion, "idi_rutas_idioma", cod.ToString(), "EDITAR", nuevo: request);
        return Respuesta.Success(await _manager.Rutas(conexion, request.cod_idioma, true), "Ruta actualizada.");
    }

    [HttpPut("GuardarAsignaturasRutaIdioma/{cod:int:min(1)}"), Authorize(Policy = Administrar)]
    public async Task<ActionResult> AsignaturasRuta(string conexion, int cod, [FromBody] RutaAsignaturasRequest request)
    {
        try
        {
            await _manager.ReemplazarAsignaturasRuta(conexion, cod, request.asignaturas);
        }
        catch (InvalidOperationException exception)
        {
            return Respuesta.Failed<object>(mensaje: exception.Message);
        }
        await _audit.Registrar(conexion, "idi_ruta_idioma_asignaturas", cod.ToString(), "REEMPLAZAR", nuevo: request);
        return Respuesta.Success(await _manager.Rutas(conexion, null, true), "Asignaturas de ruta actualizadas.");
    }

    [HttpGet("ObtenerCertificacionesIdioma"), Authorize(Policy = Consultar)]
    public async Task<ActionResult> Certificaciones(string conexion, [FromQuery] int? cod_idioma = null, [FromQuery] bool incluirInactivas = false) =>
        Respuesta.Success(await _manager.Certificaciones(conexion, cod_idioma, incluirInactivas));

    [HttpPost("CrearCertificacionIdioma"), Authorize(Policy = Administrar)]
    public async Task<ActionResult> CrearCertificacion(string conexion, [FromBody] CertificacionIdiomaRequest request)
    {
        request.codigo = request.codigo.Trim().ToUpperInvariant();
        if (await _manager.ExisteCodigo("certificaciones", request.codigo, request.cod_idioma, null, conexion) is not null)
            return Respuesta.Conflict("El código ya existe para este idioma.");
        if (!await _manager.ReferenciasValidas(request, conexion))
            return Respuesta.Failed<object>(mensaje: "El idioma o estado no es válido.");
        var cod = await _manager.CrearCertificacion(conexion, request);
        await _audit.Registrar(conexion, "idi_certificaciones_idioma", cod.ToString(), "CREAR", nuevo: request);
        return Respuesta.Success(await _manager.Certificaciones(conexion, request.cod_idioma, true), "Certificación creada.");
    }

    [HttpPut("ActualizarCertificacionIdioma/{cod:int:min(1)}"), Authorize(Policy = Administrar)]
    public async Task<ActionResult> ActualizarCertificacion(string conexion, int cod, [FromBody] CertificacionIdiomaRequest request)
    {
        if (!await _manager.Existe("certificaciones", cod, conexion))
            return Respuesta.NotFound("La certificación no existe.");
        request.codigo = request.codigo.Trim().ToUpperInvariant();
        if (await _manager.ExisteCodigo("certificaciones", request.codigo, request.cod_idioma, cod, conexion) is not null)
            return Respuesta.Conflict("El código ya existe para este idioma.");
        if (!await _manager.ReferenciasValidas(request, conexion))
            return Respuesta.Failed<object>(mensaje: "El idioma o estado no es válido.");
        await _manager.ActualizarCertificacion(conexion, cod, request);
        await _audit.Registrar(conexion, "idi_certificaciones_idioma", cod.ToString(), "EDITAR", nuevo: request);
        return Respuesta.Success(await _manager.Certificaciones(conexion, request.cod_idioma, true), "Certificación actualizada.");
    }

    [HttpPut("GuardarNivelesCertificacionIdioma/{cod:int:min(1)}"), Authorize(Policy = Administrar)]
    public async Task<ActionResult> NivelesCertificacion(string conexion, int cod, [FromBody] CertificacionNivelesRequest request)
    {
        try
        {
            await _manager.ReemplazarNivelesCertificacion(conexion, cod, request.niveles);
        }
        catch (InvalidOperationException exception)
        {
            return Respuesta.Failed<object>(mensaje: exception.Message);
        }
        await _audit.Registrar(conexion, "idi_certificacion_idioma_niveles", cod.ToString(), "REEMPLAZAR", nuevo: request);
        return Respuesta.Success(await _manager.Certificaciones(conexion, null, true), "Niveles acreditables actualizados.");
    }
}
