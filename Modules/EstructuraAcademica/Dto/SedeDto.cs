namespace UniCore.Api.Modules.EstructuraAcademica.Dto;

public sealed class SedeDto
{
    public int cod { get; set; }
    public string codigo { get; set; } = string.Empty;
    public string nombre { get; set; } = string.Empty;
    public string? direccion { get; set; }
    public string? ciudad { get; set; }
    public string? departamento { get; set; }
    public string? pais { get; set; }
    public string estado { get; set; } = string.Empty;
    public DateTime createdday { get; set; }
}
