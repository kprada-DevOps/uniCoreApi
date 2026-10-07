using System.ComponentModel.DataAnnotations;

namespace UniCore.Api.Modules.Seguridad.Requests;

public sealed class UsuarioActualizarRequest
{
    [Range(1, long.MaxValue, ErrorMessage = "La persona debe ser válida.")]
    public long? cod_persona { get; set; }

    [Required(ErrorMessage = "El username es obligatorio.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "El username debe tener entre 3 y 100 caracteres.")]
    public string username { get; set; } = string.Empty;

    public bool activo { get; set; }

    public List<int> cod_roles { get; set; } = [];
}
