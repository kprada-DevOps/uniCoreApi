using System.ComponentModel.DataAnnotations;

namespace UniCore.Api.Modules.EstructuraAcademica.Requests;

public sealed class ProgramaRequest
{
    [Range(1, int.MaxValue)] public int cod_facultad { get; set; }
    [Required, StringLength(40)] public string codigo { get; set; } = string.Empty;
    [Required, StringLength(200)] public string nombre { get; set; } = string.Empty;
    [Range(1, int.MaxValue)] public int cod_nivel { get; set; }
    [Range(1, int.MaxValue)] public int cod_modalidad { get; set; }
    [Range(1, int.MaxValue)] public int cod_tipo_periodo { get; set; }
    [Range(1, int.MaxValue)] public int duracion_periodos { get; set; }
    [StringLength(100)] public string? registro_calificado { get; set; }
    [Required, StringLength(30)] public string estado { get; set; } = "ACTIVO";
}
