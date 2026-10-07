using System.ComponentModel.DataAnnotations;

namespace UniCore.Api.Modules.Seguridad.Requests;

public sealed class UsuarioCrearRequest
{
    [Range(1, long.MaxValue, ErrorMessage = "La persona debe ser válida.")]
    public long? cod_persona { get; set; }

    [Required(ErrorMessage = "El username es obligatorio.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "El username debe tener entre 3 y 100 caracteres.")]
    public string username { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(72, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 72 caracteres.")]
    [MaxUtf8Bytes(72, ErrorMessage = "La contraseña no puede superar los 72 bytes UTF-8 que admite BCrypt.")]
    public string password { get; set; } = string.Empty;

    public bool activo { get; set; } = true;

    public List<int> cod_roles { get; set; } = [];
}
