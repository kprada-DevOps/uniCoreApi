namespace UniCore.Api.Modules.Seguridad.Dto;

/// <summary>Proyección de lectura de <c>seg_modulos_configuracion</c>.</summary>
public sealed class ModuloConfiguracionDto
{
    public int cod { get; set; }
    public int cod_modulo { get; set; }
    public string codigo { get; set; } = string.Empty;
    public string nombre { get; set; } = string.Empty;
    public string? descripcion { get; set; }
    public string? icono { get; set; }
    public int orden { get; set; }
    public bool activo { get; set; }
    public bool habilitado { get; set; }
    public DateTime? fecha_inicio { get; set; }
    public DateTime? fecha_fin { get; set; }
    public DateTime createdday { get; set; }
    public DateTime? updatedday { get; set; }
}
