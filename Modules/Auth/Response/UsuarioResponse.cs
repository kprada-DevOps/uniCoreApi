namespace UniCore.Api.Modules.Auth.Response;

/// <summary>
/// Datos públicos de un usuario. Lo que realmente viaja en la respuesta del login.
/// </summary>
public sealed class UsuarioResponse
{
    public long Cod { get; set; }

    public long? CodPersona { get; set; }

    public string Username { get; set; } = string.Empty;

    public bool Activo { get; set; }

    public DateTime? UltimoAcceso { get; set; }
}
