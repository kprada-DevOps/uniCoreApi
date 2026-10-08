using System.ComponentModel.DataAnnotations;

namespace UniCore.Api.Modules.EstructuraAcademica.Requests;

public sealed class ProgramaNivelRequest
{
    [Required, StringLength(30)] public string codigo { get; set; } = string.Empty;
    [Required, StringLength(100)] public string nombre { get; set; } = string.Empty;
    [StringLength(255)] public string? descripcion { get; set; }
    public bool activo { get; set; } = true;
}
