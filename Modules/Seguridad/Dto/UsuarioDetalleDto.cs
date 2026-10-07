namespace UniCore.Api.Modules.Seguridad.Dto;

/// <summary>Usuario administrativo con sus roles actualmente activos.</summary>
public sealed class UsuarioDetalleDto
{
    public UsuarioSeguridadDto usuario { get; set; } = new();
    public List<RolSeguridadDto> roles { get; set; } = [];
}
