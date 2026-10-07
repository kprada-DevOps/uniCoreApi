namespace UniCore.Api.Modules.Personas.Dto;

/// <summary>
/// Tipo de documento de identidad. Mapeado desde per_tipos_documento.
/// Catalogo de solo lectura: son datos de referencia sembrados por el esquema y
/// borrarlos dejaria personas sin tipo de documento valido.
/// </summary>
/// <remarks>
/// Es una de las dos unicas tablas del sector sin createdday ni updatedday, asi que
/// este DTO no lleva marcas temporales.
/// </remarks>
public sealed class TipoDocumentoDto
{
    public int cod { get; set; }

    /// <summary>Abreviatura del tipo (CC, CE, PAS...). Unica dentro del catalogo.</summary>
    public string codigo { get; set; } = string.Empty;

    public string nombre { get; set; } = string.Empty;

    public bool activo { get; set; }
}
