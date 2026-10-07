using System.ComponentModel.DataAnnotations;

namespace UniCore.Api.Modules.Personas.Requests;

/// <summary>
/// Datos para inscribir un estudiante en per_estudiantes.
/// </summary>
/// <remarks>
/// La persona debe existir antes: se referencia por <see cref="cod_persona"/> y no se
/// crea aqui. El alta de una persona tiene su propio endpoint, asi que registrar un
/// estudiante es una llamada de persona y otra de estudiante.
/// </remarks>
public class EstudianteRequest
{
    [Range(1, long.MaxValue, ErrorMessage = "La persona es obligatoria.")]
    public long cod_persona { get; set; }

    [Required(ErrorMessage = "El código de estudiante es obligatorio.")]
    [StringLength(40, ErrorMessage = "El código de estudiante no puede superar los 40 caracteres.")]
    public string codigo_estudiante { get; set; } = string.Empty;

    /// <summary>NOT NULL en el esquema, a diferencia de la fecha de ingreso del docente.</summary>
    [Required(ErrorMessage = "La fecha de ingreso es obligatoria.")]
    public DateTime? fecha_ingreso { get; set; }

    public DateTime? fecha_egreso { get; set; }

    /// <summary>
    /// El esquema lo declara VARCHAR(30) sin restriccion, asi que en vez de un enum
    /// de C# se limita el dominio con una expresion regular.
    /// </summary>
    [RegularExpression("^(ACTIVO|INACTIVO)$", ErrorMessage = "El estado debe ser ACTIVO o INACTIVO.")]
    public string? estado { get; set; }
}
