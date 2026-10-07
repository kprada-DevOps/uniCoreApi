namespace UniCore.Api.Modules.Seguridad.Dto;

/// <summary>Proyección de lectura de <c>seg_roles</c>.</summary>
public sealed class RolSeguridadDto
{
    public int cod { get; set; }
    public string codigo { get; set; } = string.Empty;
    public string nombre { get; set; } = string.Empty;
    public string? descripcion { get; set; }
    public bool activo { get; set; }
}
