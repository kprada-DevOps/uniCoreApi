using UniCore.Api.Database;
using UniCore.Api.Modules.Personas.Dto;

namespace UniCore.Api.Modules.Personas.Managers;

/// <summary>
/// Acceso a los tipos de documento de identidad.
/// Catalogo de solo lectura: lo siembra el esquema y borrar un tipo dejaria personas
/// apuntando a un codigo invalido, asi que aqui no hay alta, ni edicion, ni baja.
/// </summary>
public sealed class TipoDocumentoManager
{
    private const string Columnas = @"
    cod,
    codigo,
    nombre,
    activo";

    private const string ObtenerTiposDocumentoSql = $@"
SELECT{Columnas}
FROM per_tipos_documento
WHERE activo = 1
ORDER BY cod;";

    private const string ObtenerTipoDocumentoSql = $@"
SELECT{Columnas}
FROM per_tipos_documento
WHERE cod = @cod
LIMIT 1;";

    private readonly DatabaseProvider _database;

    public TipoDocumentoManager(DatabaseProvider database)
    {
        _database = database;
    }

    public async Task<List<TipoDocumentoDto>> ObtenerTiposDocumento(string conexion)
    {
        return await _database.GetMany<TipoDocumentoDto>(ObtenerTiposDocumentoSql, null, conexion);
    }

    /// <summary>
    /// Un tipo concreto, para validar una referencia o resolver el nombre que se
    /// muestra junto al numero de documento. No filtra por activo a proposito: una
    /// persona ya dada de alta puede seguir citando un tipo que hoy este inactivo.
    /// </summary>
    public async Task<TipoDocumentoDto?> ObtenerTipoDocumento(int cod, string conexion)
    {
        return await _database.GetOne<TipoDocumentoDto>(ObtenerTipoDocumentoSql, new { cod }, conexion);
    }
}
