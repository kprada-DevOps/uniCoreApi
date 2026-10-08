using System.ComponentModel.DataAnnotations;

namespace UniCore.Api.Modules.EstructuraAcademica.Requests;

public sealed class AulaRequest
{
    [Range(1, int.MaxValue)] public int cod_sede { get; set; }
    [Required, StringLength(40)] public string codigo { get; set; } = string.Empty;
    [Required, StringLength(120)] public string nombre { get; set; } = string.Empty;
    [Range(0, int.MaxValue)] public int capacidad { get; set; }
    [StringLength(50)] public string? tipo { get; set; }
    [Required, StringLength(30)] public string estado { get; set; } = "ACTIVA";
}
