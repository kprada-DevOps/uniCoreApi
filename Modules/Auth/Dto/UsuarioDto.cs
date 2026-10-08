namespace UniCore.Api.Modules.Auth.Dto;

/// <summary>
/// Usuario del sistema. Mapeado desde seg_usuarios.
/// El hash de la contrasena se resuelve en la capa de manager y nunca sale aqui.
/// </summary>
public sealed class UsuarioDto
{
    public long cod { get; set; }

    public long? cod_persona { get; set; }

    public string username { get; set; } = string.Empty;

    /// <summary>
    /// Hash BCrypt de la contrasena. Se excluye de la serializacion para que
    /// nunca viaje en la respuesta, ni siquiera por descuido.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string password_hash { get; set; } = string.Empty;

    public bool activo { get; set; }

    public DateTime? ultimo_acceso { get; set; }

    /// <summary>Privilegio protegido, calculado desde los roles activos del usuario.</summary>
    public bool es_superadmin { get; set; }

    /// <summary>
    /// Roles activos del usuario (codigo de seg_roles). Se rellena al autenticar.
    /// </summary>
    public List<string> roles { get; set; } = new();

    /// <summary>
    /// Permisos efectivos del usuario (codigo de seg_permisos). Se rellena al autenticar.
    /// </summary>
    public List<string> permisos { get; set; } = new();
}
