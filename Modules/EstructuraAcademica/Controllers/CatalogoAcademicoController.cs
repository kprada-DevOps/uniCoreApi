using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UniCore.Api.Helpers;
using UniCore.Api.Modules.EstructuraAcademica.Managers;

namespace UniCore.Api.Modules.EstructuraAcademica.Controllers;

public abstract class CatalogoAcademicoController<TDto, TRequest> : ControllerBase
    where TDto : class
    where TRequest : class
{
    private readonly CatalogoAcademicoManager<TDto, TRequest> _manager;

    protected CatalogoAcademicoController(CatalogoAcademicoManager<TDto, TRequest> manager)
    {
        _manager = manager;
    }

    [HttpGet]
    [Authorize(Policy = "PERMISO:ESTRUCTURA_ACADEMICA.CONSULTAR")]
    public async Task<ActionResult> Listar(string conexion, [FromQuery] bool incluirInactivos = false)
    {
        var items = await _manager.Listar(conexion, incluirInactivos);
        return Respuesta.Success(items);
    }

    [HttpGet("{cod:int:min(1)}")]
    [Authorize(Policy = "PERMISO:ESTRUCTURA_ACADEMICA.CONSULTAR")]
    public async Task<ActionResult> Obtener(string conexion, int cod)
    {
        var item = await _manager.Obtener(cod, conexion);
        if (item is null)
            return Respuesta.NotFound("El registro no existe.");

        return Respuesta.Success(item);
    }

    [HttpPost]
    [Authorize(Policy = "PERMISO:ESTRUCTURA_ACADEMICA.CREAR")]
    public async Task<ActionResult> Crear(string conexion, [FromBody] TRequest request)
    {
        var cod = await _manager.Crear(request, conexion);
        var item = await _manager.Obtener(checked((int)cod), conexion);
        return Respuesta.Success(item, "Registro creado correctamente");
    }

    [HttpPut("{cod:int:min(1)}")]
    [Authorize(Policy = "PERMISO:ESTRUCTURA_ACADEMICA.EDITAR")]
    public async Task<ActionResult> Actualizar(string conexion, int cod, [FromBody] TRequest request)
    {
        if (await _manager.Obtener(cod, conexion) is null)
            return Respuesta.NotFound("El registro no existe.");

        await _manager.Actualizar(cod, request, conexion);
        var item = await _manager.Obtener(cod, conexion);
        return Respuesta.Success(item, "Registro actualizado correctamente");
    }

    [HttpPut("{cod:int:min(1)}/Estado")]
    [Authorize(Policy = "PERMISO:ESTRUCTURA_ACADEMICA.EDITAR")]
    public async Task<ActionResult> Estado(string conexion, int cod, [FromQuery] bool activo)
    {
        if (await _manager.Obtener(cod, conexion) is null)
            return Respuesta.NotFound("El registro no existe.");

        await _manager.EstablecerActivo(cod, activo, conexion);
        var item = await _manager.Obtener(cod, conexion);
        return Respuesta.Success(item, "Estado actualizado correctamente");
    }
}
