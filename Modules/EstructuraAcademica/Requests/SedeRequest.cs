using System.ComponentModel.DataAnnotations;

namespace UniCore.Api.Modules.EstructuraAcademica.Requests;

public sealed class SedeRequest
{
    [Required, StringLength(30)] public string codigo { get; set; } = string.Empty;
    [Required, StringLength(150)] public string nombre { get; set; } = string.Empty;
    [StringLength(250)] public string? direccion { get; set; }
    [StringLength(100)] public string? ciudad { get; set; }
    [StringLength(100)] public string? departamento { get; set; }
    [StringLength(100)] public string? pais { get; set; }
    [Required, StringLength(30)] public string estado { get; set; } = "ACTIVA";
}
