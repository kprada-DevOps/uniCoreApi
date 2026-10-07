namespace UniCore.Api.Modules.Personas.Dto;

/// <summary>
/// Persona del sistema. Mapeado desde per_personas.
/// Es la entidad base de estudiantes y docentes: ambos referencian su cod.
/// </summary>
public sealed class PersonaDto
{
    public long cod { get; set; }

    /// <summary>Referencia a per_tipos_documento(cod).</summary>
    public int cod_tipo_documento { get; set; }

    /// <summary>
    /// Numero del documento. Junto con <see cref="cod_tipo_documento"/> forma el
    /// indice unico uq_per_persona_documento.
    /// </summary>
    public string numero_documento { get; set; } = string.Empty;

    public string primer_nombre { get; set; } = string.Empty;

    public string? segundo_nombre { get; set; }

    public string primer_apellido { get; set; } = string.Empty;

    public string? segundo_apellido { get; set; }

    public DateTime? fecha_nacimiento { get; set; }

    /// <summary>
    /// Sexo. VARCHAR(30) libre en el esquema: no hay ENUM ni CHECK que lo restrinja,
    /// asi que se trata como texto y no como enum.
    /// </summary>
    public string? sexo { get; set; }

    public string? email { get; set; }

    public string? telefono { get; set; }

    public string? celular { get; set; }

    public string? direccion { get; set; }

    public string? ciudad { get; set; }

    public string? departamento { get; set; }

    public string? pais { get; set; }

    /// <summary>Baja logica: la persona se marca con 0 y la fila no se borra.</summary>
    public bool activo { get; set; }

    public DateTime createdday { get; set; }

    public DateTime? updatedday { get; set; }
}
