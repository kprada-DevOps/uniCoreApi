namespace UniCore.Api.Modules.Auth.Dto;

/// <summary>
/// Respuesta del login y del refresh. Misma forma que el <c>LoginDto</c> del
/// proyecto de referencia: usuario + token + refresh.
/// </summary>
public sealed class LoginDto
{
    public UsuarioDto usuario { get; set; } = new();

    public string token { get; set; } = string.Empty;

    public string refresh { get; set; } = string.Empty;
}
