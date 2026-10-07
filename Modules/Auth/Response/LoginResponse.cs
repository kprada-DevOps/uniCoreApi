namespace UniCore.Api.Modules.Auth.Response;

/// <summary>
/// Respuesta del login: token, vigencia y perfil completo del usuario.
/// </summary>
public sealed class LoginResponse
{
    public string Token { get; set; } = string.Empty;

    public string TokenType { get; set; } = "Bearer";

    public DateTime ExpiraEn { get; set; }

    public UsuarioResponse Usuario { get; set; } = new();

    public List<string> Roles { get; set; } = new();

    public List<string> Permisos { get; set; } = new();
}
