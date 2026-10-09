using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using UniCore.Api.Database;
using UniCore.Api.Helpers;
using UniCore.Api.Modules.PlanEstudios.Managers;
using UniCore.Api.Modules.PlanEstudios.Requests;
using UniCore.Api.Modules.Seguridad.Managers;

namespace UniCore.Api.Modules.PlanEstudios.Controllers;

[ApiController, Route("{conexion}/PlanEstudios/Asignaturas"), Authorize]
public sealed class AsignaturasController : ControllerBase
{
    private readonly AsignaturasManager _manager;
    private readonly AuditoriaManager _audit;

    public AsignaturasController(AsignaturasManager manager, AuditoriaManager audit)
    {
        _manager = manager;
        _audit = audit;
    }
    [HttpGet("ObtenerAsignaturas"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.CONSULTAR")]
    public async Task<ActionResult> Listar(string conexion, [FromQuery] bool incluirInactivas = false, [FromQuery] string? busqueda = null)
        => Respuesta.Success(await _manager.Listar(conexion, incluirInactivas, busqueda));
    [HttpGet("ObtenerAsignatura/{cod:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.CONSULTAR")]
    public async Task<ActionResult> Obtener(string conexion, int cod)
    {
        var item = await _manager.Obtener(conexion, cod); if (item is null) return Respuesta.NotFound("La asignatura no existe.");
        item.prerrequisitos = await _manager.Prerrequisitos(conexion, cod); return Respuesta.Success(item);
    }
    [HttpPost("CrearAsignatura"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> Crear(string conexion, [FromBody] AsignaturaRequest r)
    {
        r.codigo = r.codigo.Trim().ToUpperInvariant();
        if (await _manager.ExisteCodigo(conexion, r.codigo, null) is not null) return Respuesta.Conflict("El código de asignatura ya existe.");
        if (!await _manager.TipoExiste(conexion, r.cod_tipo_asignatura) || !await _manager.EstadoExiste(conexion, r.cod_estado)) return Respuesta.Failed<object>(mensaje: "El tipo o estado de asignatura no es válido.");
        var cod = await _manager.Crear(conexion, r); await _audit.Registrar(conexion, "aca_asignaturas", cod.ToString(), "CREAR", nuevo: r);
        return Respuesta.Success(await _manager.Obtener(conexion, cod), "Asignatura creada correctamente.");
    }

    [HttpPost("PrevisualizarSecuencia"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> PrevisualizarSecuencia(string conexion, [FromBody] PrevisualizarAsignaturasSecuencialesRequest r)
    {
        try
        {
            return Respuesta.Success(await _manager.PrevisualizarSecuencia(conexion, r));
        }
        catch (ArgumentException ex)
        {
            return Respuesta.Failed<object>(mensaje: ex.Message);
        }
    }

    [HttpPost("CrearMasivas"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> CrearMasivas(string conexion, [FromBody] CrearAsignaturasMasivasRequest r)
    {
        var asignaturas = r.asignaturas;
        if (asignaturas.Count is < 1 or > 50)
            return Respuesta.Failed<object>(mensaje: "El lote debe contener entre 1 y 50 asignaturas.");

        foreach (var asignatura in asignaturas)
        {
            asignatura.codigo = asignatura.codigo.Trim().ToUpperInvariant();
            asignatura.nombre = asignatura.nombre.Trim();
        }

        if (asignaturas.Any(asignatura => string.IsNullOrWhiteSpace(asignatura.codigo) || string.IsNullOrWhiteSpace(asignatura.nombre)))
            return Respuesta.Failed<object>(mensaje: "Cada asignatura debe tener un código y un nombre.");

        var repetidos = asignaturas
            .GroupBy(asignatura => asignatura.codigo, StringComparer.OrdinalIgnoreCase)
            .Where(grupo => grupo.Count() > 1)
            .Select(grupo => grupo.Key)
            .ToArray();
        if (repetidos.Length > 0)
            return Respuesta.Conflict($"El lote contiene códigos repetidos: {string.Join(", ", repetidos)}.");

        if (!await _manager.ReferenciasValidas(conexion, asignaturas))
            return Respuesta.Failed<object>(mensaje: "Una o más asignaturas tienen un tipo o estado inexistente o inactivo.");

        try
        {
            List<int> codigos;
            await using (var transaccion = DatabaseConnection.BeginTransaction(conexion))
            {
                codigos = await _manager.CrearMasivas(transaccion, asignaturas);
                for (var indice = 0; indice < codigos.Count; indice++)
                {
                    await _audit.Registrar(
                        transaccion,
                        "aca_asignaturas",
                        codigos[indice].ToString(),
                        "CREAR",
                        nuevo: asignaturas.ElementAt(indice));
                }
                transaccion.Commit();
            }

            var creadas = await _manager.ObtenerPorCodigos(conexion, codigos);
            var porCodigo = creadas.ToDictionary(asignatura => asignatura.cod);
            return Respuesta.Success(
                codigos.Where(porCodigo.ContainsKey).Select(codigo => porCodigo[codigo]).ToList(),
                $"Se crearon {codigos.Count} asignaturas correctamente.");
        }
        catch (AsignaturasDuplicadasException ex)
        {
            return Respuesta.Conflict(ex.Message);
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            return Respuesta.Conflict("Uno o más códigos se registraron mientras se guardaba el lote. Actualiza la previsualización e inténtalo de nuevo.");
        }
    }

    [HttpPut("ActualizarAsignatura/{cod:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> Actualizar(string conexion, int cod, [FromBody] AsignaturaRequest r)
    {
        var antes = await _manager.Obtener(conexion, cod); if (antes is null) return Respuesta.NotFound("La asignatura no existe.");
        r.codigo = r.codigo.Trim().ToUpperInvariant();
        if (await _manager.ExisteCodigo(conexion, r.codigo, cod) is not null) return Respuesta.Conflict("El código de asignatura ya existe.");
        if (!await _manager.TipoExiste(conexion, r.cod_tipo_asignatura) || !await _manager.EstadoExiste(conexion, r.cod_estado)) return Respuesta.Failed<object>(mensaje: "El tipo o estado de asignatura no es válido.");
        await _manager.Actualizar(conexion, cod, r); await _audit.Registrar(conexion, "aca_asignaturas", cod.ToString(), "EDITAR", antes, r);
        return Respuesta.Success(await _manager.Obtener(conexion, cod), "Asignatura actualizada correctamente.");
    }
    [HttpPut("CambiarEstadoAsignatura/{cod:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> Estado(string conexion, int cod, [FromQuery] int cod_estado)
    {
        var antes = await _manager.Obtener(conexion, cod); if (antes is null) return Respuesta.NotFound("La asignatura no existe.");
        if (!await _manager.EstadoExiste(conexion, cod_estado)) return Respuesta.Failed<object>(mensaje: "El estado no existe o está inactivo.");
        await _manager.CambiarEstado(conexion, cod, cod_estado); await _audit.Registrar(conexion, "aca_asignaturas", cod.ToString(), "CAMBIAR_ESTADO", antes, new { cod_estado });
        return Respuesta.Success(await _manager.Obtener(conexion, cod), "Estado actualizado.");
    }
    [HttpGet("ObtenerPrerrequisitos/{cod:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.CONSULTAR")]
    public async Task<ActionResult> Prerrequisitos(string conexion, int cod)
        => await _manager.Obtener(conexion, cod) is null ? Respuesta.NotFound("La asignatura no existe.") : Respuesta.Success(await _manager.Prerrequisitos(conexion, cod));
    [HttpPost("AgregarPrerrequisito/{cod:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> AgregarPrerrequisito(string conexion, int cod, [FromBody] PrerrequisitoRequest r)
    {
        if (await _manager.Obtener(conexion, cod) is null) return Respuesta.NotFound("La asignatura no existe.");
        bool inserted;
        try { inserted = await _manager.AgregarPrerrequisito(conexion, cod, r); }
        catch (InvalidOperationException ex) { return Respuesta.Failed<object>(mensaje: ex.Message); }
        if (!inserted) return Respuesta.Conflict("La relación ya existe o alguna referencia no es válida.");
        await _audit.Registrar(conexion, "aca_asignatura_prerrequisitos", $"{cod}:{r.cod_asignatura_requisito}", "CREAR", nuevo: r);
        return Respuesta.Success(await _manager.Prerrequisitos(conexion, cod), "Prerrequisito agregado.");
    }

    [HttpPost("AgregarPrerrequisitos/{cod:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> AgregarPrerrequisitos(string conexion, int cod, [FromBody] PrerrequisitosMultiplesRequest r)
    {
        if (r.cod_asignaturas_requisito.Any(requisito => requisito < 1))
            return Respuesta.Failed<object>(mensaje: "Los códigos de las asignaturas requeridas deben ser válidos.");
        if (r.cod_asignaturas_requisito.Distinct().Count() != r.cod_asignaturas_requisito.Count)
            return Respuesta.Conflict("La selección contiene asignaturas repetidas.");

        bool agregados;
        try
        {
            agregados = await _manager.AgregarPrerrequisitos(
                conexion,
                cod,
                r.cod_asignaturas_requisito,
                r.cod_tipo_prerrequisito);
        }
        catch (InvalidOperationException ex)
        {
            return Respuesta.Failed<object>(mensaje: ex.Message);
        }

        if (!agregados)
            return Respuesta.Conflict("No se agregaron los prerrequisitos. Verifica que las asignaturas existan, el tipo esté activo y las relaciones no estén registradas.");

        return Respuesta.Success(await _manager.Prerrequisitos(conexion, cod), "Prerrequisitos agregados correctamente.");
    }

    [HttpDelete("EliminarPrerrequisito/{cod:int:min(1)}/{requisito:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> QuitarPrerrequisito(string conexion, int cod, int requisito)
    {
        if (!await _manager.QuitarPrerrequisito(conexion, cod, requisito)) return Respuesta.NotFound("La relación de prerrequisito no existe.");
        await _audit.Registrar(conexion, "aca_asignatura_prerrequisitos", $"{cod}:{requisito}", "ELIMINAR"); return Respuesta.Success(await _manager.Prerrequisitos(conexion, cod), "Prerrequisito eliminado.");
    }

    [HttpDelete("EliminarAsignatura/{cod:int:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.ADMINISTRAR")]
    public async Task<ActionResult> EliminarAsignatura(string conexion, int cod)
    {
        try
        {
            await using var transaccion = DatabaseConnection.BeginTransaction(conexion);
            var dependencias = await _manager.EliminarSiNoRelacionada(transaccion, cod);
            if (dependencias is null)
                return Respuesta.NotFound("La asignatura no existe.");
            if (dependencias.Count > 0)
                return Respuesta.Conflict($"No se puede eliminar porque tiene relaciones: {string.Join("; ", dependencias)}.");

            await _audit.Registrar(
                transaccion,
                "aca_asignaturas",
                cod.ToString(),
                "ELIMINAR",
                anterior: new { cod });
            transaccion.Commit();
            return Respuesta.Success<object?>(null, "Asignatura eliminada correctamente.");
        }
        catch (MySqlException ex) when (ex.Number == 1451)
        {
            return Respuesta.Conflict("No se puede eliminar la asignatura porque otra operación creó una relación con ella. Actualiza los datos e inténtalo de nuevo.");
        }
    }
}
