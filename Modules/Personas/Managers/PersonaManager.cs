using UniCore.Api.Database;
using UniCore.Api.Modules.Personas.Dto;
using UniCore.Api.Modules.Personas.Requests;

namespace UniCore.Api.Modules.Personas.Managers;

/// <summary>
/// Acceso a per_personas.
///
/// La baja es siempre logica: no hay borrado fisico. El esquema no declara ningun
/// ON DELETE, asi que todo es RESTRICT, y ademas siete tablas de otros sectores ya
/// referencian per_personas. Un DELETE fisico dejaria un 500 por violacion de FK en
/// cuanto la persona tuviera un estudiante, un docente o un usuario asociado.
/// </summary>
public sealed class PersonaManager
{
    private const string Columnas = @"
    cod,
    cod_tipo_documento,
    numero_documento,
    primer_nombre,
    segundo_nombre,
    primer_apellido,
    segundo_apellido,
    fecha_nacimiento,
    sexo,
    email,
    telefono,
    celular,
    direccion,
    ciudad,
    departamento,
    pais,
    activo,
    createdday,
    updatedday";

    private const string ObtenerPersonaSql = $@"
SELECT{Columnas}
FROM per_personas
WHERE cod = @cod
LIMIT 1;";

    /// <summary>
    /// Columnas por las que se filtra la busqueda de texto libre.
    /// </summary>
    private const string ColumnasBusqueda = @"
    numero_documento LIKE @busqueda
    OR primer_nombre LIKE @busqueda
    OR segundo_nombre LIKE @busqueda
    OR primer_apellido LIKE @busqueda
    OR segundo_apellido LIKE @busqueda";

    /// <summary>
    /// Lista de personas. Por omision solo las activas; <paramref name="incluirInactivas"/>
    /// las trae todas. <paramref name="busqueda"/> filtra por documento y nombres.
    /// </summary>
    /// <remarks>
    /// La base esta en utf8mb4_general_ci, asi que LIKE no distingue mayusculas,
    /// minusculas, tildes ni enes.
    /// </remarks>
    public async Task<List<PersonaDto>> ObtenerPersonas(
        string conexion,
        bool incluirInactivas = false,
        string? busqueda = null)
    {
        var condiciones = new List<string>();
        if (!incluirInactivas)
            condiciones.Add("activo = 1");

        if (!string.IsNullOrWhiteSpace(busqueda))
            condiciones.Add($"({ColumnasBusqueda})");

        var where = condiciones.Count > 0 ? "WHERE " + string.Join(" AND ", condiciones) : string.Empty;
        var query = $@"
SELECT{Columnas}
FROM per_personas
{where}
ORDER BY primer_apellido, primer_nombre;";

        return await DatabaseConnection.GetMany<PersonaDto>(conexion, query, new { busqueda = string.IsNullOrWhiteSpace(busqueda) ? null : $"%{busqueda}%" });
    }

    public async Task<PersonaDto?> ObtenerPersona(long cod, string conexion)
    {
        return await DatabaseConnection.GetOne<PersonaDto>(conexion, ObtenerPersonaSql, new { cod });
    }

    /// <summary>
    /// Da de alta una persona y devuelve su cod.
    /// No se envia createdday ni updatedday: los deja el DEFAULT CURRENT_TIMESTAMP
    /// de MySQL, porque el manager construye la sentencia a partir de las
    /// propiedades del objeto y bastaria con no incluirlas.
    /// </summary>
    public async Task<long> CrearPersona(PersonaRequest request, string conexion)
    {
        var datos = new
        {
            request.cod_tipo_documento,
            request.numero_documento,
            request.primer_nombre,
            request.segundo_nombre,
            request.primer_apellido,
            request.segundo_apellido,
            request.fecha_nacimiento,
            request.sexo,
            request.email,
            request.telefono,
            request.celular,
            request.direccion,
            request.ciudad,
            request.departamento,
            request.pais,
            request.activo,
        };

        return await DatabaseConnection.Insert(conexion, "per_personas", datos);
    }

    /// <summary>
    /// Reemplaza los datos de una persona. Semantica de PUT: los campos opcionales
    /// que llegan en null se limpian. Reactivar una persona es llamar aqui con
    /// activo en true.
    /// </summary>
    public async Task<bool> ActualizarPersona(long cod, PersonaActualizarRequest request, string conexion)
    {
        var datos = new
        {
            cod,
            request.cod_tipo_documento,
            request.numero_documento,
            request.primer_nombre,
            request.segundo_nombre,
            request.primer_apellido,
            request.segundo_apellido,
            request.fecha_nacimiento,
            request.sexo,
            request.email,
            request.telefono,
            request.celular,
            request.direccion,
            request.ciudad,
            request.departamento,
            request.pais,
            request.activo,
            updatedday = DbHelpers.GetFechaActualDatetime(conexion),
        };

        return await DatabaseConnection.Update(conexion, "per_personas", datos, new { cod }, true);
    }

    /// <summary>
    /// Baja logica: marca la persona como inactiva sin borrar la fila, para no romper
    /// los estudiantes, docentes, usuarios y registros de otros sectores que la
    /// referencian.
    /// </summary>
    public async Task<bool> DesactivarPersona(long cod, string conexion)
    {
        var datos = new
        {
            cod,
            activo = false,
            updatedday = DbHelpers.GetFechaActualDatetime(conexion),
        };

        return await DatabaseConnection.Update(conexion, "per_personas", datos, new { cod }, true);
    }
}
