using System.ComponentModel.DataAnnotations;

namespace UniCore.Api.Modules.Idiomas.Requests;

public sealed class IdiomaRequest
{
    [Required, StringLength(20)] public string codigo { get; set; } = string.Empty;
    [Required, StringLength(100)] public string nombre { get; set; } = string.Empty;
    [Range(1, int.MaxValue)] public int cod_estado { get; set; }
}

public sealed class NivelIdiomaRequest
{
    [Range(1, int.MaxValue)] public int cod_estado { get; set; }
}

public sealed class FormatoNivelIdiomaRequest
{
    [Required, RegularExpression("^(ROMANO|ARABIGO)$")] public string formato_nivel { get; set; } = "ROMANO";
}

public sealed class RutaIdiomaRequest
{
    [Range(1, int.MaxValue)] public int cod_idioma { get; set; }
    [Required, StringLength(50)] public string codigo { get; set; } = string.Empty;
    [Required, StringLength(150)] public string nombre { get; set; } = string.Empty;
    public string? descripcion { get; set; }
    [Range(1, int.MaxValue)] public int cod_nivel_final { get; set; }
    [Range(1, int.MaxValue)] public int cod_estado { get; set; }
}

public sealed class RutaAsignaturasRequest
{
    [Required] public List<RutaAsignaturaRequest> asignaturas { get; set; } = [];
}

public sealed class RutaAsignaturaRequest
{
    [Range(1, int.MaxValue)] public int cod_nivel_idioma { get; set; }
    [Required, StringLength(40)] public string codigo { get; set; } = string.Empty;
    public string? descripcion { get; set; }
    [Range(typeof(decimal), "0", "999.99", ParseLimitsInInvariantCulture = true)] public decimal creditos { get; set; }
    [Range(typeof(decimal), "0", "9999.99", ParseLimitsInInvariantCulture = true)] public decimal horas_teoricas { get; set; }
    [Range(typeof(decimal), "0", "9999.99", ParseLimitsInInvariantCulture = true)] public decimal horas_practicas { get; set; }
    [Range(1, int.MaxValue)] public int cod_estado { get; set; }
}

public sealed class CertificacionIdiomaRequest
{
    [Range(1, int.MaxValue)] public int cod_idioma { get; set; }
    [Required, StringLength(50)] public string codigo { get; set; } = string.Empty;
    [Required, StringLength(180)] public string nombre { get; set; } = string.Empty;
    [StringLength(180)] public string? entidad_emisora { get; set; }
    public string? descripcion { get; set; }
    [Range(1, 1200)] public int? vigencia_meses { get; set; }
    [Range(1, int.MaxValue)] public int cod_estado { get; set; }
}

public sealed class CertificacionNivelesRequest
{
    [Required] public List<CertificacionNivelRequest> niveles { get; set; } = [];
}

public sealed class CertificacionNivelRequest
{
    [Range(1, int.MaxValue)] public int cod_nivel_idioma { get; set; }
    [Range(typeof(decimal), "0", "99999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal? puntaje_minimo { get; set; }
    [Range(typeof(decimal), "0", "99999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal? puntaje_maximo { get; set; }
}
