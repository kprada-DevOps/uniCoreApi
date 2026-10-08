using System.ComponentModel.DataAnnotations;

namespace UniCore.Api.Modules.EstructuraAcademica.Requests;

public sealed class DepartamentoRequest
{
    [Range(1, int.MaxValue)] public int cod_facultad { get; set; }
    [Required, StringLength(30)] public string codigo { get; set; } = string.Empty;
    [Required, StringLength(180)] public string nombre { get; set; } = string.Empty;
    [Required, StringLength(30)] public string estado { get; set; } = "ACTIVO";
}
