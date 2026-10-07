using System.ComponentModel.DataAnnotations;

namespace UniCore.Api.Modules.Seguridad.Requests;

public sealed class RolRequest
{
    [Required, StringLength(50, MinimumLength = 2)]
    [RegularExpression("^[A-Za-z0-9._-]+$", ErrorMessage = "El código solo admite letras, números, punto, guion y guion bajo.")]
    public string codigo { get; set; } = string.Empty;

    [Required, StringLength(120, MinimumLength = 2)]
    public string nombre { get; set; } = string.Empty;

    [StringLength(250)]
    public string? descripcion { get; set; }

    public bool activo { get; set; } = true;

    public List<int> cod_permisos { get; set; } = [];
}
