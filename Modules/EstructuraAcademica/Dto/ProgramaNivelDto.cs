namespace UniCore.Api.Modules.EstructuraAcademica.Dto;

public sealed class ProgramaNivelDto
{
    public int cod { get; set; }
    public string codigo { get; set; } = string.Empty;
    public string nombre { get; set; } = string.Empty;
    public string? descripcion { get; set; }
    public bool activo { get; set; }
    public DateTime createdday { get; set; }
    public DateTime? updatedday { get; set; }
}
