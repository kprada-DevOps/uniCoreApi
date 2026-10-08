namespace UniCore.Api.Modules.EstructuraAcademica.Dto;

public sealed class ProgramaDto
{
    public int cod { get; set; }
    public int cod_facultad { get; set; }
    public string codigo { get; set; } = string.Empty;
    public string nombre { get; set; } = string.Empty;
    public int cod_nivel { get; set; }
    public int cod_modalidad { get; set; }
    public int cod_tipo_periodo { get; set; }
    public int duracion_periodos { get; set; }
    public string? registro_calificado { get; set; }
    public string estado { get; set; } = string.Empty;
    public DateTime createdday { get; set; }
    public DateTime? updatedday { get; set; }
    public string facultad_nombre { get; set; } = string.Empty;
    public string nivel_nombre { get; set; } = string.Empty;
    public string modalidad_nombre { get; set; } = string.Empty;
    public string tipo_periodo_nombre { get; set; } = string.Empty;
}
