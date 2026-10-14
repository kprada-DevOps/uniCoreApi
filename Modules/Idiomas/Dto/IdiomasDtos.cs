namespace UniCore.Api.Modules.Idiomas.Dto;

public sealed class CatalogoIdiomaDto
{
    public int cod { get; set; }
    public string nombre { get; set; } = string.Empty;
}

public sealed class IdiomaDto
{
    public int cod { get; set; }
    public string codigo { get; set; } = string.Empty;
    public string nombre { get; set; } = string.Empty;
    public int cod_estado { get; set; }
    public string estado { get; set; } = string.Empty;
}

public sealed class NivelIdiomaDto
{
    public int cod { get; set; }
    public string codigo { get; set; } = string.Empty;
    public string nombre { get; set; } = string.Empty;
    public int orden { get; set; }
    public int cod_estado { get; set; }
    public string estado { get; set; } = string.Empty;
}

public sealed class FormatoNivelIdiomaDto
{
    public string formato_nivel { get; set; } = "ROMANO";
}

public sealed class AsignaturaRutaIdiomaDto
{
    public int cod { get; set; }
    public string codigo { get; set; } = string.Empty;
    public string nombre { get; set; } = string.Empty;
}

public sealed class RutaIdiomaDto
{
    public int cod { get; set; }
    public int cod_idioma { get; set; }
    public string idioma { get; set; } = string.Empty;
    public string codigo { get; set; } = string.Empty;
    public string nombre { get; set; } = string.Empty;
    public string? descripcion { get; set; }
    public int cod_nivel_final { get; set; }
    public int cod_estado { get; set; }
    public string estado { get; set; } = string.Empty;
    public List<RutaIdiomaAsignaturaDto> asignaturas { get; set; } = [];
}

public sealed class RutaIdiomaAsignaturaDto
{
    public long cod { get; set; }
    public int cod_ruta_idioma { get; set; }
    public int cod_idioma_asignatura { get; set; }
    public string codigo_asignatura { get; set; } = string.Empty;
    public string asignatura { get; set; } = string.Empty;
    public int? cod_nivel_idioma { get; set; }
    public string? nivel_idioma { get; set; }
    public int? nivel_orden { get; set; }
    public string? descripcion { get; set; }
    public decimal creditos { get; set; }
    public decimal horas_teoricas { get; set; }
    public decimal horas_practicas { get; set; }
    public int orden { get; set; }
    public int cod_estado { get; set; }
}

public sealed class CertificacionIdiomaDto
{
    public int cod { get; set; }
    public int cod_idioma { get; set; }
    public string idioma { get; set; } = string.Empty;
    public string codigo { get; set; } = string.Empty;
    public string nombre { get; set; } = string.Empty;
    public string? entidad_emisora { get; set; }
    public string? descripcion { get; set; }
    public int? vigencia_meses { get; set; }
    public int cod_estado { get; set; }
    public string estado { get; set; } = string.Empty;
    public List<CertificacionNivelDto> niveles { get; set; } = [];
}

public sealed class CertificacionNivelDto
{
    public long cod { get; set; }
    public int cod_certificacion { get; set; }
    public int cod_nivel_idioma { get; set; }
    public string nivel { get; set; } = string.Empty;
    public int nivel_orden { get; set; }
    public decimal? puntaje_minimo { get; set; }
    public decimal? puntaje_maximo { get; set; }
}
