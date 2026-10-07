namespace UniCore.Api.Modules.Seguridad.Dto;

public sealed class RolDetalleDto
{
    public RolSeguridadDto rol { get; set; } = new();
    public List<PermisoSeguridadDto> permisos { get; set; } = [];
}
