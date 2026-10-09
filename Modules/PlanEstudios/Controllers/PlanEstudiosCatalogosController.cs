using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniCore.Api.Helpers;
using UniCore.Api.Modules.PlanEstudios.Managers;
using UniCore.Api.Modules.PlanEstudios.Requests;
using UniCore.Api.Modules.Seguridad.Managers;

namespace UniCore.Api.Modules.PlanEstudios.Controllers;

[ApiController, Route("{conexion}/PlanEstudios/Catalogos"), Authorize]
public sealed class PlanEstudiosCatalogosController : ControllerBase
{
    private readonly PlanEstudiosCatalogosManager _manager;
    private readonly AuditoriaManager _audit;

    public PlanEstudiosCatalogosController(PlanEstudiosCatalogosManager manager, AuditoriaManager audit)
    {
        _manager = manager;
        _audit = audit;
    }
    [HttpGet("ObtenerCatalogo/{catalogo}")]
    [Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.CONSULTAR")]
    public async Task<ActionResult> Listar(string conexion, string catalogo, [FromQuery] bool incluirInactivos = false)
    {
        if (!PlanEstudiosCatalogosManager.EsCatalogoValido(catalogo)) return Respuesta.NotFound("El catálogo de Plan de Estudios no existe.");
        return Respuesta.Success(await _manager.Listar(conexion, catalogo, incluirInactivos));
    }

    [HttpGet("ObtenerRegistroCatalogo/{catalogo}/{cod:int:min(1)}")]
    [Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.CONSULTAR")]
    public async Task<ActionResult> Obtener(string conexion, string catalogo, int cod)
    {
        if (!PlanEstudiosCatalogosManager.EsCatalogoValido(catalogo)) return Respuesta.NotFound("El catálogo de Plan de Estudios no existe.");
        var item = await _manager.Obtener(conexion, catalogo, cod);
        return item is null ? Respuesta.NotFound("El registro no existe.") : Respuesta.Success(item);
    }

    [HttpPost("CrearCatalogo/{catalogo}")]
    [Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> Crear(string conexion, string catalogo, [FromBody] CatalogoPlanRequest request)
    {
        if (!PlanEstudiosCatalogosManager.EsCatalogoValido(catalogo)) return Respuesta.NotFound("El catálogo de Plan de Estudios no existe.");
        if (!PlanEstudiosCatalogosManager.DatosValidos(catalogo, request)) return Respuesta.Failed<object>(mensaje: "El código o nombre supera la longitud permitida para este catálogo.");
        request.codigo = PlanEstudiosCatalogosManager.Normalizar(request.codigo);
        if (await _manager.ExisteCodigo(conexion, catalogo, request.codigo, null) is not null) return Respuesta.Conflict("El código ya está registrado en el catálogo.");
        var cod = await _manager.Crear(conexion, catalogo, request);
        await _audit.Registrar(conexion, "catalogo_" + catalogo, cod.ToString(), "CREAR", nuevo: request);
        return Respuesta.Success(await _manager.Obtener(conexion, catalogo, cod), "Registro creado correctamente.");
    }

    [HttpPut("ActualizarCatalogo/{catalogo}/{cod:int:min(1)}")]
    [Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> Actualizar(string conexion, string catalogo, int cod, [FromBody] CatalogoPlanRequest request)
    {
        if (!PlanEstudiosCatalogosManager.EsCatalogoValido(catalogo)) return Respuesta.NotFound("El catálogo de Plan de Estudios no existe.");
        if (!PlanEstudiosCatalogosManager.DatosValidos(catalogo, request)) return Respuesta.Failed<object>(mensaje: "El código o nombre supera la longitud permitida para este catálogo.");
        var antes = await _manager.Obtener(conexion, catalogo, cod); if (antes is null) return Respuesta.NotFound("El registro no existe.");
        request.codigo = PlanEstudiosCatalogosManager.Normalizar(request.codigo);
        if (await _manager.ExisteCodigo(conexion, catalogo, request.codigo, cod) is not null) return Respuesta.Conflict("El código ya está registrado en el catálogo.");
        await _manager.Actualizar(conexion, catalogo, cod, request);
        await _audit.Registrar(conexion, "catalogo_" + catalogo, cod.ToString(), "EDITAR", antes, request);
        return Respuesta.Success(await _manager.Obtener(conexion, catalogo, cod), "Registro actualizado correctamente.");
    }

    [HttpPut("CambiarEstadoCatalogo/{catalogo}/{cod:int:min(1)}")]
    [Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> Estado(string conexion, string catalogo, int cod, [FromQuery] bool activo)
    {
        if (!PlanEstudiosCatalogosManager.EsCatalogoValido(catalogo)) return Respuesta.NotFound("El catálogo de Plan de Estudios no existe.");
        var antes = await _manager.Obtener(conexion, catalogo, cod); if (antes is null) return Respuesta.NotFound("El registro no existe.");
        await _manager.Estado(conexion, catalogo, cod, activo);
        await _audit.Registrar(conexion, "catalogo_" + catalogo, cod.ToString(), activo ? "ACTIVAR" : "DESACTIVAR", antes, new { activo });
        return Respuesta.Success(await _manager.Obtener(conexion, catalogo, cod), "Estado actualizado.");
    }
}
