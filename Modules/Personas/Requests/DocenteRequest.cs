using System.ComponentModel.DataAnnotations;

namespace UniCore.Api.Modules.Personas.Requests;

/// <summary>
/// Datos para registrar un docente en per_docentes.
/// </summary>
/// <remarks>
/// La persona debe existir antes: se referencia por <see cref="cod_persona"/>.
/// A diferencia del estudiante, la fecha de ingreso es nullable en el esquema.
/// </remarks>
public class DocenteRequest
{
    [Range(1, long.MaxValue, ErrorMessage = "La persona es obligatoria.")]
    public long cod_persona { get; set; }

    [Required(ErrorMessage = "El código de docente es obligatorio.")]
    [StringLength(40, ErrorMessage = "El código de docente no puede superar los 40 caracteres.")]
    public string codigo_docente { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "El tipo de vinculación no puede superar los 50 caracteres.")]
    public string? tipo_vinculacion { get; set; }

    /// <summary>NULL en el esquema, a diferencia de per_estudiantes.</summary>
    public DateTime? fecha_ingreso { get; set; }

    /// <summary>Se llama fecha_retiro en docentes; en estudiantes es fecha_egreso.</summary>
    public DateTime? fecha_retiro { get; set; }

    [RegularExpression("^(ACTIVO|INACTIVO)$", ErrorMessage = "El estado debe ser ACTIVO o INACTIVO.")]
    public string? estado { get; set; }
}
