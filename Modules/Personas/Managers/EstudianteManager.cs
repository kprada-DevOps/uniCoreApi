using UniCore.Api.Database;
using UniCore.Api.Modules.Personas.Dto;
using UniCore.Api.Modules.Personas.Requests;

namespace UniCore.Api.Modules.Personas.Managers;

/// <summary>
/// Acceso a per_estudiantes, unido a su persona y a su tipo de documento.
///
/// La baja es logica mediante la columna estado, que sustituye al campo activo que si
/// tiene per_personas: el esquema lo declara VARCHAR(30) sin CHECK, asi que no hay
/// enum y el dominio se limita a ACTIVO e INACTIVO.
/// </summary>
public sealed class EstudianteManager
{
    /// <summary>Unico valor de estado que considera el sistema.</summary>
    public const string EstadoActivo = "ACTIVO";

    public const string EstadoInactivo = "INACTIVO";

    /// <summary>
    /// Proyeccion con alias explicitos: hay tres tablas unidas y dos de ellas
    /// comparten nombres de columna (cod y nombre), que hay que renombrar.
    /// </summary>
    private const string Columnas = @"
    e.cod                  AS cod,
    e.cod_persona          AS cod_persona,
    e.codigo_estudiante    AS codigo_estudiante,
    e.fecha_ingreso        AS fecha_ingreso,
    e.fecha_egreso         AS fecha_egreso,
    e.estado               AS estado,
    e.createdday           AS createdday,
    e.updatedday           AS updatedday,
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
    p.activo               AS persona_activo,
    CONCAT_WS(' ', p.primer_nombre, p.segundo_nombre, p.primer_apellido, p.segundo_apellido) AS nombre_completo";

    private const string Origen = @"
FROM per_estudiantes e
INNER JOIN per_personas p ON p.cod = e.cod_persona
INNER JOIN per_tipos_documento t ON t.cod = p.cod_tipo_documento";

    private const string ObtenerEstudianteSql = $@"
SELECT{Columnas}
{Origen}
WHERE e.cod = @cod
LIMIT 1;";

    private const string ColumnasBusqueda = @"
    e.codigo_estudiante LIKE @busqueda
    OR p.numero_documento LIKE @busqueda
    OR p.primer_nombre LIKE @busqueda
    OR p.primer_apellido LIKE @busqueda";

    public async Task<List<EstudianteDto>> ObtenerEstudiantes(
        string conexion,
        bool incluirInactivos = false,
        string? busqueda = null)
    {
        var condiciones = new List<string>();
        if (!incluirInactivos)
            condiciones.Add($"e.estado = '{EstadoActivo}'");

        if (!string.IsNullOrWhiteSpace(busqueda))
            condiciones.Add($"({ColumnasBusqueda})");

        var where = condiciones.Count > 0 ? "WHERE " + string.Join(" AND ", condiciones) : string.Empty;
        var query = $@"
SELECT{Columnas}
{Origen}
{where}
ORDER BY p.primer_apellido, p.primer_nombre;";

        return await DatabaseConnection.GetMany<EstudianteDto>(conexion, query, new { busqueda = string.IsNullOrWhiteSpace(busqueda) ? null : $"%{busqueda}%" });
    }

    public async Task<EstudianteDto?> ObtenerEstudiante(long cod, string conexion)
    {
        return await DatabaseConnection.GetOne<EstudianteDto>(conexion, ObtenerEstudianteSql, new { cod });
    }

    /// <summary>
    /// Inscribe un estudiante. La persona debe existir: se referencia por
    /// <c>cod_persona</c> y no se crea aqui.
    /// </summary>
    public async Task<long> CrearEstudiante(EstudianteRequest request, string conexion)
    {
        var datos = new
        {
            request.cod_persona,
            request.codigo_estudiante,
            // La fecha de ingreso es NOT NULL en el esquema y la validacion la
            // garantiza, pero se resuelve aqui para no dejar un DateTime? sin valor.
            fecha_ingreso = request.fecha_ingreso!.Value,
            request.fecha_egreso,
            // estado es NOT NULL con DEFAULT 'ACTIVO'. Insert no omite los null, asi
            // que se resuelve el valor por defecto en lugar de confiar en MySQL.
            estado = request.estado ?? EstadoActivo,
        };

        return await DatabaseConnection.Insert(conexion, "per_estudiantes", datos);
    }

    public async Task<bool> ActualizarEstudiante(long cod, EstudianteActualizarRequest request, string conexion)
    {
        var datos = new
        {
            cod,
            request.cod_persona,
            request.codigo_estudiante,
            fecha_ingreso = request.fecha_ingreso!.Value,
            request.fecha_egreso,
            estado = request.estado ?? EstadoActivo,
            updatedday = DbHelpers.GetFechaActualDatetime(conexion),
        };

        return await DatabaseConnection.Update(conexion, "per_estudiantes", datos, new { cod }, true);
    }

    /// <summary>
    /// Baja logica: pasa el estado a INACTIVO sin borrar la fila.
    /// No toca el activo de la persona asociada a proposito: una misma persona podria
    /// ser docente y estudiante a la vez, y desactivar el estudiante no implica
    /// desactivar a la persona.
    /// </summary>
    public async Task<bool> DesactivarEstudiante(long cod, string conexion)
    {
        var datos = new
        {
            cod,
            estado = EstadoInactivo,
            updatedday = DbHelpers.GetFechaActualDatetime(conexion),
        };

        return await DatabaseConnection.Update(conexion, "per_estudiantes", datos, new { cod }, true);
    }
}
