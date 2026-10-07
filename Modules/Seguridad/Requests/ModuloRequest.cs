using System.ComponentModel.DataAnnotations;

namespace UniCore.Api.Modules.Seguridad.Requests;

public sealed class ModuloRequest
{
    [Required, StringLength(50, MinimumLength = 2)]
    [RegularExpression("^[A-Za-z0-9._-]+$")] public string codigo { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 2)] public string nombre { get; set; } = string.Empty;
    [StringLength(300)] public string? descripcion { get; set; }
    [StringLength(100)] public string? icono { get; set; }
    public int orden { get; set; }
    public bool activo { get; set; } = true;
    public bool habilitado { get; set; } = true;
    public DateTime? fecha_inicio { get; set; }
    public DateTime? fecha_fin { get; set; }
}
