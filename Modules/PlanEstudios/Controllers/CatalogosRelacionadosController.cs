using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniCore.Api.Helpers;
using UniCore.Api.Modules.PlanEstudios.Managers;
using UniCore.Api.Modules.PlanEstudios.Requests;
using UniCore.Api.Modules.Seguridad.Managers;

namespace UniCore.Api.Modules.PlanEstudios.Controllers;

[ApiController, Route("{conexion}/PlanEstudios"), Authorize]
public sealed class CatalogosRelacionadosController : ControllerBase
{
    private readonly CatalogosRelacionadosManager _manager;
    private readonly AuditoriaManager _audit;

    public CatalogosRelacionadosController(CatalogosRelacionadosManager manager, AuditoriaManager audit)
    {
        _manager = manager;
        _audit = audit;
    }
    [HttpGet("ObtenerIdiomas"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.CONSULTAR")]
    public async Task<ActionResult> Idiomas(string conexion, [FromQuery] bool incluirInactivos = false) => Respuesta.Success(await _manager.Idiomas(conexion, incluirInactivos));
    [HttpPost("CrearIdioma"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> CrearIdioma(string conexion, [FromBody] IdiomaRequest r) { r.codigo = r.codigo.Trim().ToUpperInvariant(); if (await _manager.ExisteCodigo("idiomas", r.codigo, null, null, conexion) is not null) return Respuesta.Conflict("El código del idioma ya existe."); if (!await _manager.ReferenciasValidas(r, conexion)) return Respuesta.Failed<object>(mensaje: "El estado no existe o está inactivo."); var id = await _manager.CrearIdioma(conexion, r); await _audit.Registrar(conexion, "aca_idiomas", id.ToString(), "CREAR", nuevo: r); var items = await _manager.Idiomas(conexion, true); return Respuesta.Success(items.FirstOrDefault(x => x.cod == id), "Idioma creado."); }
    [HttpPut("ActualizarIdioma/{cod:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> ActualizarIdioma(string conexion, int cod, [FromBody] IdiomaRequest r) { if (!await _manager.Existe("idiomas", cod, conexion)) return Respuesta.NotFound("El idioma no existe."); r.codigo = r.codigo.Trim().ToUpperInvariant(); if (await _manager.ExisteCodigo("idiomas", r.codigo, null, cod, conexion) is not null) return Respuesta.Conflict("El código del idioma ya existe."); if (!await _manager.ReferenciasValidas(r, conexion)) return Respuesta.Failed<object>(mensaje: "El estado no existe o está inactivo."); await _manager.ActualizarIdioma(conexion, cod, r); await _audit.Registrar(conexion, "aca_idiomas", cod.ToString(), "EDITAR", nuevo: r); return Respuesta.Success(await _manager.Idiomas(conexion, true), "Idioma actualizado."); }

    [HttpGet("ObtenerNivelesIdioma"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.CONSULTAR")]
    public async Task<ActionResult> Niveles(string conexion, [FromQuery] bool incluirInactivos = false) => Respuesta.Success(await _manager.Niveles(conexion, incluirInactivos));
    [HttpPost("CrearNivelIdioma"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> CrearNivel(string conexion, [FromBody] NivelIdiomaRequest r) { r.codigo = r.codigo.Trim().ToUpperInvariant(); if (await _manager.ExisteCodigo("niveles", r.codigo, null, null, conexion) is not null) return Respuesta.Conflict("El código del nivel ya existe."); if (!await _manager.ReferenciasValidas(r, conexion)) return Respuesta.Failed<object>(mensaje: "El estado no existe o está inactivo."); var id = await _manager.CrearNivel(conexion, r); await _audit.Registrar(conexion, "aca_niveles_idioma", id.ToString(), "CREAR", nuevo: r); return Respuesta.Success(id, "Nivel de idioma creado."); }
    [HttpPut("ActualizarNivelIdioma/{cod:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> ActualizarNivel(string conexion, int cod, [FromBody] NivelIdiomaRequest r) { if (!await _manager.Existe("niveles", cod, conexion)) return Respuesta.NotFound("El nivel no existe."); r.codigo = r.codigo.Trim().ToUpperInvariant(); if (await _manager.ExisteCodigo("niveles", r.codigo, null, cod, conexion) is not null) return Respuesta.Conflict("El código del nivel ya existe."); if (!await _manager.ReferenciasValidas(r, conexion)) return Respuesta.Failed<object>(mensaje: "El estado no existe o está inactivo."); await _manager.ActualizarNivel(conexion, cod, r); await _audit.Registrar(conexion, "aca_niveles_idioma", cod.ToString(), "EDITAR", nuevo: r); return Respuesta.Success(await _manager.Niveles(conexion, true), "Nivel de idioma actualizado."); }

    [HttpGet("ObtenerRutasIdioma"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.CONSULTAR")]
    public async Task<ActionResult> Rutas(string conexion, [FromQuery] int? cod_idioma = null, [FromQuery] bool incluirInactivos = false) => Respuesta.Success(await _manager.Rutas(conexion, cod_idioma, incluirInactivos));
    [HttpPost("CrearRutaIdioma"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> CrearRuta(string conexion, [FromBody] RutaIdiomaRequest r)
    {
        r.codigo = r.codigo.Trim().ToUpperInvariant();

        if (await _manager.ExisteCodigo("rutas", r.codigo, r.cod_idioma, null, conexion) is not null)
            return Respuesta.Conflict("El código ya existe para este idioma.");

        if (!await _manager.ReferenciasValidas(r, conexion))
            return Respuesta.Failed<object>(mensaje: "El idioma, nivel final o estado no es válido.");

        var id = await _manager.CrearRuta(conexion, r);
        await _audit.Registrar(conexion, "aca_rutas_idioma", id.ToString(), "CREAR", nuevo: r);
        return Respuesta.Success(await _manager.Rutas(conexion, r.cod_idioma, true), "Ruta creada.");
    }
    [HttpPut("ActualizarRutaIdioma/{cod:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> ActualizarRuta(string conexion, int cod, [FromBody] RutaIdiomaRequest r) { if (!await _manager.Existe("rutas", cod, conexion)) return Respuesta.NotFound("La ruta no existe."); r.codigo = r.codigo.Trim().ToUpperInvariant(); if (await _manager.ExisteCodigo("rutas", r.codigo, r.cod_idioma, cod, conexion) is not null) return Respuesta.Conflict("El código ya existe para este idioma."); if (!await _manager.ReferenciasValidas(r, conexion)) return Respuesta.Failed<object>(mensaje: "El idioma, nivel final o estado no es válido."); await _manager.ActualizarRuta(conexion, cod, r); await _audit.Registrar(conexion, "aca_rutas_idioma", cod.ToString(), "EDITAR", nuevo: r); return Respuesta.Success(await _manager.Rutas(conexion, r.cod_idioma, true), "Ruta actualizada."); }
    [HttpPut("GuardarAsignaturasRutaIdioma/{cod:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> AsignaturasRuta(string conexion, int cod, [FromBody] RutaAsignaturasRequest r) { try { await _manager.ReemplazarAsignaturasRuta(conexion, cod, r.asignaturas); } catch (InvalidOperationException ex) { return Respuesta.Failed<object>(mensaje: ex.Message); } await _audit.Registrar(conexion, "aca_ruta_idioma_asignaturas", cod.ToString(), "REEMPLAZAR", nuevo: r); return Respuesta.Success(await _manager.Rutas(conexion, null, true), "Asignaturas de ruta actualizadas."); }

    [HttpGet("ObtenerCertificacionesIdioma"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.CONSULTAR")]
    public async Task<ActionResult> Certificaciones(string conexion, [FromQuery] int? cod_idioma = null, [FromQuery] bool incluirInactivos = false) => Respuesta.Success(await _manager.Certificaciones(conexion, cod_idioma, incluirInactivos));
    [HttpPost("CrearCertificacionIdioma"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> CrearCertificacion(string conexion, [FromBody] CertificacionIdiomaRequest r) { r.codigo = r.codigo.Trim().ToUpperInvariant(); if (await _manager.ExisteCodigo("certificaciones", r.codigo, r.cod_idioma, null, conexion) is not null) return Respuesta.Conflict("El código ya existe para este idioma."); if (!await _manager.ReferenciasValidas(r, conexion)) return Respuesta.Failed<object>(mensaje: "El idioma o estado no es válido."); var id = await _manager.CrearCertificacion(conexion, r); await _audit.Registrar(conexion, "aca_certificaciones_idioma", id.ToString(), "CREAR", nuevo: r); return Respuesta.Success(await _manager.Certificaciones(conexion, r.cod_idioma, true), "Certificación creada."); }
    [HttpPut("ActualizarCertificacionIdioma/{cod:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> ActualizarCertificacion(string conexion, int cod, [FromBody] CertificacionIdiomaRequest r) { if (!await _manager.Existe("certificaciones", cod, conexion)) return Respuesta.NotFound("La certificación no existe."); r.codigo = r.codigo.Trim().ToUpperInvariant(); if (await _manager.ExisteCodigo("certificaciones", r.codigo, r.cod_idioma, cod, conexion) is not null) return Respuesta.Conflict("El código ya existe para este idioma."); if (!await _manager.ReferenciasValidas(r, conexion)) return Respuesta.Failed<object>(mensaje: "El idioma o estado no es válido."); await _manager.ActualizarCertificacion(conexion, cod, r); await _audit.Registrar(conexion, "aca_certificaciones_idioma", cod.ToString(), "EDITAR", nuevo: r); return Respuesta.Success(await _manager.Certificaciones(conexion, r.cod_idioma, true), "Certificación actualizada."); }
    [HttpPut("GuardarNivelesCertificacionIdioma/{cod:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> NivelesCertificacion(string conexion, int cod, [FromBody] CertificacionNivelesRequest r) { try { await _manager.ReemplazarNivelesCertificacion(conexion, cod, r.niveles); } catch (InvalidOperationException ex) { return Respuesta.Failed<object>(mensaje: ex.Message); } await _audit.Registrar(conexion, "aca_certificacion_idioma_niveles", cod.ToString(), "REEMPLAZAR", nuevo: r); return Respuesta.Success(await _manager.Certificaciones(conexion, null, true), "Niveles acreditables actualizados."); }

    [HttpGet("ObtenerActividadesCocurriculares"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.CONSULTAR")]
    public async Task<ActionResult> Actividades(string conexion, [FromQuery] bool incluirInactivos = false) => Respuesta.Success(await _manager.Actividades(conexion, incluirInactivos));
    [HttpPost("CrearActividadCocurricular"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> CrearActividad(string conexion, [FromBody] ActividadCocurricularRequest r) { r.codigo = r.codigo.Trim().ToUpperInvariant(); if (await _manager.ExisteCodigo("actividades", r.codigo, null, null, conexion) is not null) return Respuesta.Conflict("El código de actividad ya existe."); if (!await _manager.ReferenciasValidas(r, conexion)) return Respuesta.Failed<object>(mensaje: "El tipo de actividad o estado no es válido."); var id = await _manager.CrearActividad(conexion, r); await _audit.Registrar(conexion, "aca_actividades_cocurriculares", id.ToString(), "CREAR", nuevo: r); return Respuesta.Success(await _manager.Actividades(conexion, true), "Actividad creada."); }
    [HttpPut("ActualizarActividadCocurricular/{cod:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> ActualizarActividad(string conexion, int cod, [FromBody] ActividadCocurricularRequest r) { if (!await _manager.Existe("actividades", cod, conexion)) return Respuesta.NotFound("La actividad no existe."); r.codigo = r.codigo.Trim().ToUpperInvariant(); if (await _manager.ExisteCodigo("actividades", r.codigo, null, cod, conexion) is not null) return Respuesta.Conflict("El código de actividad ya existe."); if (!await _manager.ReferenciasValidas(r, conexion)) return Respuesta.Failed<object>(mensaje: "El tipo de actividad o estado no es válido."); await _manager.ActualizarActividad(conexion, cod, r); await _audit.Registrar(conexion, "aca_actividades_cocurriculares", cod.ToString(), "EDITAR", nuevo: r); return Respuesta.Success(await _manager.Actividades(conexion, true), "Actividad actualizada."); }
}
