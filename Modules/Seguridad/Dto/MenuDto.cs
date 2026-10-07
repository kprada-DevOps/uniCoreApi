namespace UniCore.Api.Modules.Seguridad.Dto;

/// <summary>Proyección de lectura de <c>seg_menu</c>; la jerarquía se expresa con <c>cod_padre</c>.</summary>
public sealed class MenuDto
{
    public int cod { get; set; }
    public int cod_modulo { get; set; }
    public int? cod_padre { get; set; }
    public string codigo { get; set; } = string.Empty;
    public string nombre { get; set; } = string.Empty;
    public string? descripcion { get; set; }
    public string? icono { get; set; }
    public string? ruta { get; set; }
    public int orden { get; set; }
    public string tipo { get; set; } = "ITEM";
    public bool activo { get; set; }
    public DateTime createdday { get; set; }
    public DateTime? updatedday { get; set; }
    public List<MenuDto> hijos { get; set; } = [];
}
