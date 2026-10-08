namespace UniCore.Api.Modules.EstructuraAcademica.Dto;

public sealed class FacultadDto
{
    public int cod { get; set; }
    public int cod_sede { get; set; }
    public string codigo { get; set; } = string.Empty;
    public string nombre { get; set; } = string.Empty;
    public string estado { get; set; } = string.Empty;
    public DateTime createdday { get; set; }
    public string sede_nombre { get; set; } = string.Empty;
}
