namespace UniCore.Api.Modules.Personas.Dto;

/// <summary>
/// Docente. Mapeado desde per_docentes unido a su persona y a su tipo de documento.
/// </summary>
/// <remarks>
/// No comparte clase base con <see cref="EstudianteDto"/>: aqui fecha_ingreso es
/// NULL y la fecha de salida se llama fecha_retiro. Ademas esta tabla declara
/// tipo_vinculacion, que no existe en estudiantes.
/// </remarks>
public sealed class DocenteDto
{
    public long cod { get; set; }

    /// <summary>Referencia a per_personas(cod), unica por docente.</summary>
    public long cod_persona { get; set; }

    public string codigo_docente { get; set; } = string.Empty;

    /// <summary>Tipo de vinculacion laboral. Exclusivo de esta tabla.</summary>
    public string? tipo_vinculacion { get; set; }

    /// <summary>NULL en el esquema, a diferencia de per_estudiantes.</summary>
    public DateTime? fecha_ingreso { get; set; }

    /// <summary>Fecha de retiro. NULL mientras siga activo.</summary>
    public DateTime? fecha_retiro { get; set; }

    /// <summary>
    /// Estado del docente. VARCHAR(30) libre: el esquema no lo restringe, asi que
    /// solo se usan los valores ACTIVO e INACTIVO, sin enum en C#.
    /// </summary>
    public string estado { get; set; } = string.Empty;

    public DateTime createdday { get; set; }

    public DateTime? updatedday { get; set; }

    // ---------- Datos de la persona asociada ----------

    public string numero_documento { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de documento de la persona. La tabla per_docentes no lo tiene: viene del
    /// join con per_personas, y el formulario lo necesita para no perderlo al editar.
    /// </summary>
    public int cod_tipo_documento { get; set; }

    public string tipo_documento_codigo { get; set; } = string.Empty;

    public string tipo_documento_nombre { get; set; } = string.Empty;

    public string primer_nombre { get; set; } = string.Empty;

    public string? segundo_nombre { get; set; }

    public string primer_apellido { get; set; } = string.Empty;

    public string? segundo_apellido { get; set; }

    public string? email { get; set; } = string.Empty;

    public string? telefono { get; set; }

    public string? celular { get; set; }

    public DateTime? fecha_nacimiento { get; set; }

    /// <summary>Activo de la persona asociada. Distinto del estado del docente.</summary>
    public bool persona_activa { get; set; }

    public string nombre_completo { get; set; } = string.Empty;
}
