using UniCore.Api.Database;
using UniCore.Api.Modules.Personas.Dto;
using UniCore.Api.Modules.Personas.Requests;

namespace UniCore.Api.Modules.Personas.Managers;

/// <summary>
/// Acceso a per_docentes, unido a su persona y a su tipo de documento.
/// </summary>
/// <remarks>
/// A diferencia de <see cref="EstudianteManager"/>, aqui la fecha de ingreso es
/// nullable y la de salida se llama fecha_retiro.
/// </remarks>
public sealed class DocenteManager
{
    public const string EstadoActivo = "ACTIVO";

    public const string EstadoInactivo = "INACTIVO";

    private const string Columnas = @"
    d.cod                  AS cod,
    d.cod_persona          AS cod_persona,
    d.codigo_docente       AS codigo_docente,
    d.tipo_vinculacion     AS tipo_vinculacion,
    d.fecha_ingreso        AS fecha_ingreso,
    d.fecha_retiro         AS fecha_retiro,
    d.estado               AS estado,
    d.createdday           AS createdday,
    d.updatedday           AS updatedday,
    p.numero_documento     AS numero_documento,
    p.cod_tipo_documento   AS cod_tipo_documento,
    t.codigo               AS tipo_documento_codigo,
    t.nombre               AS tipo_documento_nombre,
    p.primer_nombre        AS primer_nombre,
    p.segundo_nombre       AS segundo_nombre,
    p.primer_apellido      AS primer_apellido,
    p.segundo_apellido     AS segundo_apellido,
    p.email                AS email,
    p.telefono             AS telefono,
    p.celular              AS celular,
    p.fecha_nacimiento     AS fecha_nacimiento,
    p.activo               AS persona_activa,
    CONCAT_WS(' ', p.primer_nombre, p.segundo_nombre, p.primer_apellido, p.segundo_apellido) AS nombre_completo";

    private const string Origen = @"
FROM per_docentes d
INNER JOIN per_personas p ON p.cod = d.cod_persona
INNER JOIN per_tipos_documento t ON t.cod = p.cod_tipo_documento";

    private const string ObtenerDocenteSql = $@"
SELECT{Columnas}
{Origen}
WHERE d.cod = @cod
LIMIT 1;";

    private const string ColumnasBusqueda = @"
    d.codigo_docente LIKE @busqueda
    OR p.numero_documento LIKE @busqueda
    OR p.primer_nombre LIKE @busqueda
    OR p.primer_apellido LIKE @busqueda";

    private readonly DatabaseProvider _database;

    public DocenteManager(DatabaseProvider database)
    {
        _database = database;
    }

    public async Task<List<DocenteDto>> ObtenerDocentes(
        string conexion,
        bool incluirInactivos = false,
        string? busqueda = null)
    {
        var condiciones = new List<string>();
        if (!incluirInactivos)
            condiciones.Add($"d.estado = '{EstadoActivo}'");

        if (!string.IsNullOrWhiteSpace(busqueda))
            condiciones.Add($"({ColumnasBusqueda})");

        var where = condiciones.Count > 0 ? "WHERE " + string.Join(" AND ", condiciones) : string.Empty;
        var query = $@"
SELECT{Columnas}
{Origen}
{where}
ORDER BY p.primer_apellido, p.primer_nombre;";

        return await _database.GetMany<DocenteDto>(
            query,
            new { busqueda = string.IsNullOrWhiteSpace(busqueda) ? null : $"%{busqueda}%" },
            conexion);
    }

    public async Task<DocenteDto?> ObtenerDocente(long cod, string conexion)
    {
        return await _database.GetOne<DocenteDto>(ObtenerDocenteSql, new { cod }, conexion);
    }

    /// <summary>
    /// Registra un docente. La persona debe existir: se referencia por
    /// <c>cod_persona</c> y no se crea aqui.
    /// </summary>
    public async Task<long> CrearDocente(DocenteRequest request, string conexion)
    {
        var datos = new
        {
            request.cod_persona,
            request.codigo_docente,
            request.tipo_vinculacion,
            // A diferencia del estudiante, aqui fecha_ingreso admite null.
            request.fecha_ingreso,
            request.fecha_retiro,
            estado = request.estado ?? EstadoActivo,
        };

        return await _database.Insert("per_docentes", datos, conexion);
    }

    public async Task<bool> ActualizarDocente(long cod, DocenteActualizarRequest request, string conexion)
    {
        var datos = new
        {
            cod,
            request.cod_persona,
            request.codigo_docente,
            request.tipo_vinculacion,
            request.fecha_ingreso,
            request.fecha_retiro,
            estado = request.estado ?? EstadoActivo,
            updatedday = DbHelpers.GetFechaActualDatetime(conexion),
        };

        return await _database.Update("per_docentes", datos, new { cod }, true, conexion);
    }

    /// <summary>
    /// Baja logica: pasa el estado a INACTIVO sin borrar la fila. Si tiene fecha de
    /// ingreso y no la de retiro, se deja la de ingreso como esta: la baja no implica
    /// que el docente se haya retirado de la institucion, solo que deja de impartir.
    /// </summary>
    public async Task<bool> DesactivarDocente(long cod, string conexion)
    {
        var datos = new
        {
            cod,
            estado = EstadoInactivo,
            updatedday = DbHelpers.GetFechaActualDatetime(conexion),
        };

        return await _database.Update("per_docentes", datos, new { cod }, true, conexion);
    }
}
