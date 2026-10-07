using System.ComponentModel.DataAnnotations;

namespace UniCore.Api.Modules.Seguridad.Requests;

public sealed class MenuRequest
{
    [Range(1, int.MaxValue)] public int cod_modulo { get; set; }
    public int? cod_padre { get; set; }
    [Required, StringLength(100, MinimumLength = 2)]
    [RegularExpression("^[A-Za-z0-9._-]+$")] public string codigo { get; set; } = string.Empty;
    [Required, StringLength(150, MinimumLength = 2)] public string nombre { get; set; } = string.Empty;
    [StringLength(300)] public string? descripcion { get; set; }
    [StringLength(100)] public string? icono { get; set; }
    [StringLength(250)] public string? ruta { get; set; }
    public int orden { get; set; }
    [Required, StringLength(30)] public string tipo { get; set; } = "ITEM";
    public bool activo { get; set; } = true;
}
