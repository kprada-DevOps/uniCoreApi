using System.ComponentModel.DataAnnotations;

namespace UniCore.Api.Modules.Seguridad.Requests;

public sealed class CambiarContrasenaRequest
{
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(72, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 72 caracteres.")]
    [MaxUtf8Bytes(72, ErrorMessage = "La contraseña no puede superar los 72 bytes UTF-8 que admite BCrypt.")]
    public string password { get; set; } = string.Empty;
}
