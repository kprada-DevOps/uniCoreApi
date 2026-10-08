using System.ComponentModel.DataAnnotations;

namespace UniCore.Api.Modules.PlanEstudios.Requests;

public sealed class CatalogoPlanRequest
{
    [Required, StringLength(50)] public string codigo { get; set; } = string.Empty;
    [Required, StringLength(120)] public string nombre { get; set; } = string.Empty;
    [StringLength(255)] public string? descripcion { get; set; }
    public bool activo { get; set; } = true;
}

public sealed class AsignaturaRequest
{
    [Required, StringLength(40)] public string codigo { get; set; } = string.Empty;
    [Required, StringLength(180)] public string nombre { get; set; } = string.Empty;
    public string? descripcion { get; set; }
    [Range(typeof(decimal), "0", "999.99")] public decimal creditos { get; set; }
    [Range(typeof(decimal), "0", "9999.99")] public decimal horas_teoricas { get; set; }
    [Range(typeof(decimal), "0", "9999.99")] public decimal horas_practicas { get; set; }
    [Range(1, int.MaxValue)] public int cod_tipo_asignatura { get; set; }
    [Range(1, int.MaxValue)] public int cod_estado { get; set; }
}

public sealed class PrerrequisitoRequest
{
    [Range(1, int.MaxValue)] public int cod_asignatura_requisito { get; set; }
    [Range(1, int.MaxValue)] public int cod_tipo_prerrequisito { get; set; }
}

public sealed class PlanEstudioRequest
{
    [Range(1, int.MaxValue)] public int cod_programa { get; set; }
    [Required, StringLength(40)] public string codigo { get; set; } = string.Empty;
    [Required, StringLength(180)] public string nombre { get; set; } = string.Empty;
    [Required, StringLength(30)] public string version { get; set; } = string.Empty;
    public DateTime fecha_inicio { get; set; }
    public DateTime? fecha_fin { get; set; }
    [Range(1, int.MaxValue)] public int cod_estado { get; set; }
}

public sealed class PlanElementoRequest
{
    public int? cod_asignatura { get; set; }
    public int? cod_componente { get; set; }
    [Range(1, 100)] public int semestre { get; set; }
    [Range(typeof(decimal), "0", "999.99")] public decimal creditos { get; set; }
    public int? orden { get; set; }
    [Range(1, int.MaxValue)] public int cod_estado { get; set; }
}

public sealed class PlanRequisitoRequest
{
    [Range(1, int.MaxValue)] public int cod_requisito { get; set; }
    public bool obligatorio { get; set; } = true;
    public int? orden { get; set; }
    [Range(1, int.MaxValue)] public int cod_estado { get; set; }
}

public sealed class ComponenteRequest
{
    [Required, StringLength(50)] public string codigo { get; set; } = string.Empty;
    [Required, StringLength(180)] public string nombre { get; set; } = string.Empty;
    public string? descripcion { get; set; }
    [Range(1, int.MaxValue)] public int cod_tipo_componente { get; set; }
    [Range(1, int.MaxValue)] public int cod_comportamiento { get; set; }
    [Range(typeof(decimal), "0", "999.99")] public decimal creditos { get; set; }
    [Range(1, int.MaxValue)] public int cod_estado { get; set; }
}

public sealed class ComponenteOpcionesRequest
{
    [Required, MinLength(1)] public List<ComponenteOpcionRequest> opciones { get; set; } = [];
}

public sealed class ComponenteOpcionRequest
{
    [Range(1, int.MaxValue)] public int cod_asignatura { get; set; }
    public int? orden { get; set; }
    [Range(1, int.MaxValue)] public int cod_estado { get; set; }
}

public sealed class RequisitoCocurricularRequest
{
    [Required, StringLength(50)] public string codigo { get; set; } = string.Empty;
    [Required, StringLength(180)] public string nombre { get; set; } = string.Empty;
    public string? descripcion { get; set; }
    [Range(1, int.MaxValue)] public int cod_tipo_requisito { get; set; }
    [Range(1, int.MaxValue)] public int cod_comportamiento { get; set; }
    [Range(typeof(decimal), "0", "999999.99")] public decimal? cantidad { get; set; }
    public int? cod_unidad_medida { get; set; }
    [Range(1, int.MaxValue)] public int cod_estado { get; set; }
}

public sealed class MecanismoRequisitoRequest
{
    [Range(1, int.MaxValue)] public int cod_tipo_mecanismo { get; set; }
    [StringLength(180)] public string? nombre { get; set; }
    public string? descripcion { get; set; }
    public bool obligatorio { get; set; }
    [Range(1, int.MaxValue)] public int cod_estado { get; set; }
    public List<int> rutas { get; set; } = [];
}

public sealed class RequisitoIdiomaRequest
{
    [Range(1, int.MaxValue)] public int cod_idioma { get; set; }
    [Range(1, int.MaxValue)] public int cod_nivel_minimo { get; set; }
    public List<int> rutas { get; set; } = [];
    public List<int> certificaciones { get; set; } = [];
}

public sealed class RequisitoActividadRequest
{
    [Range(1, int.MaxValue)] public int cod_actividad { get; set; }
}

public sealed class ReemplazarRelacionesRequest<T>
{
    public List<T> items { get; set; } = [];
}

public sealed class CumplimientoRequest
{
    [Range(1, int.MaxValue)] public int cod_estado_cumplimiento { get; set; }
    public DateTime? fecha_inicio { get; set; }
    public DateTime? fecha_cumplimiento { get; set; }
    public string? observacion { get; set; }
}

public sealed class EvidenciaRequest
{
    public long? cod_requisito_mecanismo { get; set; }
    [Range(1, int.MaxValue)] public int cod_tipo_evidencia { get; set; }
    [Required, StringLength(180)] public string nombre { get; set; } = string.Empty;
    [StringLength(255)] public string? referencia { get; set; }
    [StringLength(180)] public string? resultado { get; set; }
    public DateTime? fecha_emision { get; set; }
    public DateTime? fecha_vencimiento { get; set; }
    public string? observacion { get; set; }
}

public sealed class IdiomaRequest { [Required, StringLength(20)] public string codigo { get; set; } = string.Empty; [Required, StringLength(100)] public string nombre { get; set; } = string.Empty; [Range(1, int.MaxValue)] public int cod_estado { get; set; } }
public sealed class NivelIdiomaRequest { [Required, StringLength(20)] public string codigo { get; set; } = string.Empty; [Required, StringLength(100)] public string nombre { get; set; } = string.Empty; [Range(1, 100)] public int orden { get; set; } [Range(1, int.MaxValue)] public int cod_estado { get; set; } }
public sealed class RutaIdiomaRequest { [Range(1, int.MaxValue)] public int cod_idioma { get; set; } [Required, StringLength(50)] public string codigo { get; set; } = string.Empty; [Required, StringLength(150)] public string nombre { get; set; } = string.Empty; public string? descripcion { get; set; } public int? cod_nivel_final { get; set; } [Range(1, int.MaxValue)] public int cod_estado { get; set; } }
public sealed class RutaAsignaturasRequest { [Required] public List<RutaAsignaturaRequest> asignaturas { get; set; } = []; }
public sealed class RutaAsignaturaRequest { [Range(1, int.MaxValue)] public int cod_asignatura { get; set; } public int? cod_nivel_idioma { get; set; } [Range(1, int.MaxValue)] public int orden { get; set; } [Range(1, int.MaxValue)] public int cod_estado { get; set; } }
public sealed class CertificacionIdiomaRequest { [Range(1, int.MaxValue)] public int cod_idioma { get; set; } [Required, StringLength(50)] public string codigo { get; set; } = string.Empty; [Required, StringLength(180)] public string nombre { get; set; } = string.Empty; [StringLength(180)] public string? entidad_emisora { get; set; } public string? descripcion { get; set; } [Range(1, 1200)] public int? vigencia_meses { get; set; } [Range(1, int.MaxValue)] public int cod_estado { get; set; } }
public sealed class CertificacionNivelesRequest { [Required] public List<CertificacionNivelRequest> niveles { get; set; } = []; }
public sealed class CertificacionNivelRequest { [Range(1, int.MaxValue)] public int cod_nivel_idioma { get; set; } public decimal? puntaje_minimo { get; set; } public decimal? puntaje_maximo { get; set; } }
public sealed class ActividadCocurricularRequest { [Required, StringLength(50)] public string codigo { get; set; } = string.Empty; [Required, StringLength(180)] public string nombre { get; set; } = string.Empty; public string? descripcion { get; set; } [Range(1, int.MaxValue)] public int cod_tipo_actividad { get; set; } [Range(1, int.MaxValue)] public int cod_estado { get; set; } }
