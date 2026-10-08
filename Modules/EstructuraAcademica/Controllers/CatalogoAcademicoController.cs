using Microsoft.AspNetCore.Mvc;
using UniCore.Api.Helpers;
using UniCore.Api.Modules.EstructuraAcademica.Managers;

namespace UniCore.Api.Modules.EstructuraAcademica.Controllers;

public abstract class CatalogoAcademicoController<TDto, TRequest>(
    CatalogoAcademicoManager<TDto, TRequest> manager) : ControllerBase
    where TDto : class
    where TRequest : class
{
    [HttpGet]
    public async Task<ActionResult> Listar(string conexion, [FromQuery] bool incluirInactivos = false)
        => Respuesta.Success(await manager.Listar(conexion, incluirInactivos));

    [HttpGet("{cod:int:min(1)}")]
    public async Task<ActionResult> Obtener(string conexion, int cod)
    {
        var item = await manager.Obtener(cod, conexion);
        return item is null ? Respuesta.NotFound("El registro no existe.") : Respuesta.Success(item);
    }

    [HttpPost]
    public async Task<ActionResult> Crear(string conexion, [FromBody] TRequest request)
    {
        var cod = await manager.Crear(request, conexion);
        return Respuesta.Success(await manager.Obtener(checked((int)cod), conexion), "Registro creado correctamente");
    }

    [HttpPut("{cod:int:min(1)}")]
    public async Task<ActionResult> Actualizar(string conexion, int cod, [FromBody] TRequest request)
    {
        if (await manager.Obtener(cod, conexion) is null)
            return Respuesta.NotFound("El registro no existe.");

        await manager.Actualizar(cod, request, conexion);
        return Respuesta.Success(await manager.Obtener(cod, conexion), "Registro actualizado correctamente");
    }

    [HttpPut("{cod:int:min(1)}/Estado")]
    public async Task<ActionResult> Estado(string conexion, int cod, [FromQuery] bool activo)
    {
        if (await manager.Obtener(cod, conexion) is null)
            return Respuesta.NotFound("El registro no existe.");

        await manager.EstablecerActivo(cod, activo, conexion);
        return Respuesta.Success(await manager.Obtener(cod, conexion), "Estado actualizado correctamente");
    }
}
