namespace UniCore.Api.Modules.Seguridad.Dto;

/// <summary>Proyección de lectura de <c>seg_permisos</c>.</summary>
public sealed class PermisoSeguridadDto
{
    public int cod { get; set; }
    public int cod_modulo { get; set; }
    public string codigo { get; set; } = string.Empty;
    public string nombre { get; set; } = string.Empty;
    /// <summary>Acción admitida por el ENUM definido en UniCoreBDv1.1.</summary>
    public string accion { get; set; } = string.Empty;
    public bool activo { get; set; }
    public string? descripcion { get; set; }
    public DateTime createdday { get; set; }
}
