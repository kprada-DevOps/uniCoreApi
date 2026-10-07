using System.ComponentModel.DataAnnotations;

namespace UniCore.Api.Modules.Auth.Requests;

/// <summary>
/// Credenciales de acceso. Nombres de campo identicos a los del proyecto de
/// referencia, para que el frontend consuma ambos sin cambios.
/// </summary>
public sealed class LoginUsuarioRequest
{
    [Required(ErrorMessage = "El usuario es obligatorio.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "El usuario debe tener entre 3 y 100 caracteres.")]
    public string user { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(200, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 200 caracteres.")]
    public string password { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de usuario solicitado. El esquema de UniCore no almacena un tipo en
    /// seg_usuarios (los roles vienen de seg_usuario_roles), asi que de momento se
    /// acepta y se ignora, igual que hara falta si se anade la columna.
    /// </summary>
    [Required(ErrorMessage = "El tipo de usuario es obligatorio.")]
    public string tipo { get; set; } = string.Empty;

    /// <summary>
    /// Medio de acceso usado (1 = usuario y contrasena). Reservado para los otros
    /// medios de autenticacion que tenga el proyecto de referencia.
    /// </summary>
    public string? medio_acceso { get; set; }
}
