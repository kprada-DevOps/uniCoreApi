using System.ComponentModel.DataAnnotations;

namespace UniCore.Api.Modules.Seguridad.Requests;

public sealed class PermisoRequest
{
    [Range(1, int.MaxValue)] public int cod_modulo { get; set; }
    [Required, StringLength(100, MinimumLength = 2)]
    [RegularExpression("^[A-Za-z0-9._-]+$")] public string codigo { get; set; } = string.Empty;
    [Required, StringLength(150, MinimumLength = 2)] public string nombre { get; set; } = string.Empty;
    [Required, RegularExpression("^(VER|CONSULTAR|CREAR|EDITAR|GESTIONAR|CANCELAR|ELIMINAR|EXPORTAR|IMPORTAR|APROBAR|ANULAR)$")]
    public string accion { get; set; } = "CONSULTAR";
    [StringLength(255)] public string? descripcion { get; set; }
    public bool activo { get; set; } = true;
}
