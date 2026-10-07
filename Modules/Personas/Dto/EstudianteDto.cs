namespace UniCore.Api.Modules.Personas.Dto;

/// <summary>
/// Estudiante. Mapeado desde per_estudiantes unido a su persona y a su tipo de
/// documento, porque en pantalla siempre se muestran juntos.
/// </summary>
/// <remarks>
/// No comparte clase base con <see cref="DocenteDto"/>: aqui fecha_ingreso es
/// NOT NULL y la fecha de salida se llama fecha_egreso, mientras que en docentes
/// fecha_ingreso es NULL y se llama fecha_retiro.
/// </remarks>
public sealed class EstudianteDto
{
    public long cod { get; set; }

    /// <summary>Referencia a per_personas(cod), unica por estudiante.</summary>
    public long cod_persona { get; set; }

    public string codigo_estudiante { get; set; } = string.Empty;

    /// <summary>NOT NULL en el esquema.</summary>
    public DateTime fecha_ingreso { get; set; }

    /// <summary>Fecha de egreso. NULL mientras siga activo.</summary>
    public DateTime? fecha_egreso { get; set; }

    /// <summary>
    /// Estado del estudiante. VARCHAR(30) libre: el esquema no lo restringe, asi que
    /// solo se usan los valores ACTIVO e INACTIVO, sin enum en C#.
    /// </summary>
    public string estado { get; set; } = string.Empty;

    public DateTime createdday { get; set; }

    public DateTime? updatedday { get; set; }

    // ---------- Datos de la persona asociada ----------

    public string numero_documento { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de documento de la persona. La tabla per_estudiantes no lo tiene: viene del
    /// join con per_personas, y el formulario lo necesita para no perderlo al editar.
    /// </summary>
    public int cod_tipo_documento { get; set; }

    public string tipo_documento_codigo { get; set; } = string.Empty;

    public string tipo_documento_nombre { get; set; } = string.Empty;

    public string primer_nombre { get; set; } = string.Empty;

    public string? segundo_nombre { get; set; }

    public string primer_apellido { get; set; } = string.Empty;

    public string? segundo_apellido { get; set; }

    public string? email { get; set; }

    public string? telefono { get; set; }

    public string? celular { get; set; }

    public DateTime? fecha_nacimiento { get; set; }

    /// <summary>Activo de la persona asociada. Distinto del estado del estudiante.</summary>
    public bool persona_activa { get; set; }

    /// <summary>
    /// Nombre completo compuesto en SQL, para no repetir el mismo CONCAT_WS en
    /// cada componente.
    /// </summary>
    public string nombre_completo { get; set; } = string.Empty;
}
