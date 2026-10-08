namespace UniCore.Api.Modules.PlanEstudios.Dto;

public sealed class CatalogoPlanDto
{
    public int cod { get; set; }
    public string codigo { get; set; } = string.Empty;
    public string nombre { get; set; } = string.Empty;
    public string? descripcion { get; set; }
    public bool activo { get; set; }
}

public sealed class AsignaturaDto
{
    public int cod { get; set; }
    public string codigo { get; set; } = string.Empty;
    public string nombre { get; set; } = string.Empty;
    public string? descripcion { get; set; }
    public decimal creditos { get; set; }
    public decimal horas_teoricas { get; set; }
    public decimal horas_practicas { get; set; }
    public int cod_tipo_asignatura { get; set; }
    public string tipo_asignatura { get; set; } = string.Empty;
    public int cod_estado { get; set; }
    public string estado { get; set; } = string.Empty;
    public DateTime createdday { get; set; }
    public DateTime? updatedday { get; set; }
    public List<PrerrequisitoDto> prerrequisitos { get; set; } = [];
}

public sealed class PrerrequisitoDto
{
    public int cod_asignatura { get; set; }
    public string codigo_asignatura { get; set; } = string.Empty;
    public string nombre_asignatura { get; set; } = string.Empty;
    public int cod_asignatura_requisito { get; set; }
    public string codigo_requisito { get; set; } = string.Empty;
    public string nombre_requisito { get; set; } = string.Empty;
    public int cod_tipo_prerrequisito { get; set; }
    public string tipo_prerrequisito { get; set; } = string.Empty;
}

public sealed class PlanEstudioDto
{
    public int cod { get; set; }
    public int cod_programa { get; set; }
    public string programa { get; set; } = string.Empty;
    public string codigo { get; set; } = string.Empty;
    public string nombre { get; set; } = string.Empty;
    public string version { get; set; } = string.Empty;
    public DateTime fecha_inicio { get; set; }
    public DateTime? fecha_fin { get; set; }
    public int cod_estado { get; set; }
    public string estado { get; set; } = string.Empty;
    public DateTime createdday { get; set; }
    public DateTime? updatedday { get; set; }
    public List<PlanElementoDto> elementos { get; set; } = [];
    public List<PlanRequisitoDto> requisitos { get; set; } = [];
}

public sealed class PlanElementoDto
{
    public long cod { get; set; }
    public int cod_plan_estudio { get; set; }
    public int? cod_asignatura { get; set; }
    public string? asignatura_codigo { get; set; }
    public string? asignatura_nombre { get; set; }
    public int? cod_componente { get; set; }
    public string? componente_codigo { get; set; }
    public string? componente_nombre { get; set; }
    public int semestre { get; set; }
    public decimal creditos { get; set; }
    public int? orden { get; set; }
    public int cod_estado { get; set; }
    public string estado { get; set; } = string.Empty;
    public List<ComponenteOpcionDto> opciones { get; set; } = [];
}

public sealed class PlanRequisitoDto
{
    public long cod { get; set; }
    public int cod_plan_estudio { get; set; }
    public int cod_requisito { get; set; }
    public string codigo_requisito { get; set; } = string.Empty;
    public string requisito { get; set; } = string.Empty;
    public bool obligatorio { get; set; }
    public int? orden { get; set; }
    public int cod_estado { get; set; }
    public string estado { get; set; } = string.Empty;
}

public sealed class ComponenteDto
{
    public int cod { get; set; }
    public string codigo { get; set; } = string.Empty;
    public string nombre { get; set; } = string.Empty;
    public string? descripcion { get; set; }
    public int cod_tipo_componente { get; set; }
    public string tipo_componente { get; set; } = string.Empty;
    public int cod_comportamiento { get; set; }
    public string comportamiento { get; set; } = string.Empty;
    public decimal creditos { get; set; }
    public int cod_estado { get; set; }
    public string estado { get; set; } = string.Empty;
    public List<ComponenteOpcionDto> opciones { get; set; } = [];
}

public sealed class ComponenteOpcionDto
{
    public long cod { get; set; }
    public int cod_componente { get; set; }
    public int cod_asignatura { get; set; }
    public string codigo_asignatura { get; set; } = string.Empty;
    public string asignatura { get; set; } = string.Empty;
    public int? orden { get; set; }
    public int cod_estado { get; set; }
    public string estado { get; set; } = string.Empty;
}

public sealed class RequisitoCocurricularDto
{
    public int cod { get; set; }
    public string codigo { get; set; } = string.Empty;
    public string nombre { get; set; } = string.Empty;
    public string? descripcion { get; set; }
    public int cod_tipo_requisito { get; set; }
    public string tipo_requisito { get; set; } = string.Empty;
    public int cod_comportamiento { get; set; }
    public string comportamiento { get; set; } = string.Empty;
    public decimal? cantidad { get; set; }
    public int? cod_unidad_medida { get; set; }
    public string? unidad_medida { get; set; }
    public int cod_estado { get; set; }
    public string estado { get; set; } = string.Empty;
    public List<RequisitoMecanismoDto> mecanismos { get; set; } = [];
    public List<RequisitoIdiomaDto> idiomas { get; set; } = [];
    public List<RequisitoActividadDto> actividades { get; set; } = [];
}

public sealed class RequisitoMecanismoDto
{
    public long cod { get; set; }
    public int cod_requisito { get; set; }
    public int cod_tipo_mecanismo { get; set; }
    public string tipo_mecanismo { get; set; } = string.Empty;
    public string? nombre { get; set; }
    public string? descripcion { get; set; }
    public bool obligatorio { get; set; }
    public int cod_estado { get; set; }
    public List<int> rutas { get; set; } = [];
}

public sealed class RequisitoIdiomaDto
{
    public long cod { get; set; }
    public int cod_requisito { get; set; }
    public int cod_idioma { get; set; }
    public string idioma { get; set; } = string.Empty;
    public int cod_nivel_minimo { get; set; }
    public string nivel_minimo { get; set; } = string.Empty;
    public List<int> rutas { get; set; } = [];
    public List<int> certificaciones { get; set; } = [];
}

public sealed class RequisitoActividadDto
{
    public int cod_requisito { get; set; }
    public int cod_actividad { get; set; }
    public string codigo { get; set; } = string.Empty;
    public string nombre { get; set; } = string.Empty;
    public int cod_tipo_actividad { get; set; }
}

public sealed class CumplimientoEstudianteDto
{
    public long cod { get; set; }
    public long cod_estudiante { get; set; }
    public string codigo_estudiante { get; set; } = string.Empty;
    public long cod_plan_requisito { get; set; }
    public int cod_plan_estudio { get; set; }
    public string plan_estudio { get; set; } = string.Empty;
    public long cod_requisito { get; set; }
    public string requisito { get; set; } = string.Empty;
    public int cod_estado_cumplimiento { get; set; }
    public string estado_cumplimiento { get; set; } = string.Empty;
    public DateTime? fecha_inicio { get; set; }
    public DateTime? fecha_cumplimiento { get; set; }
    public string? observacion { get; set; }
    public List<EvidenciaRequisitoDto> evidencias { get; set; } = [];
}

public sealed class IdiomaDto { public int cod { get; set; } public string codigo { get; set; } = string.Empty; public string nombre { get; set; } = string.Empty; public int cod_estado { get; set; } public string estado { get; set; } = string.Empty; }
public sealed class NivelIdiomaDto { public int cod { get; set; } public string codigo { get; set; } = string.Empty; public string nombre { get; set; } = string.Empty; public int orden { get; set; } public int cod_estado { get; set; } public string estado { get; set; } = string.Empty; }
public sealed class RutaIdiomaDto { public int cod { get; set; } public int cod_idioma { get; set; } public string idioma { get; set; } = string.Empty; public string codigo { get; set; } = string.Empty; public string nombre { get; set; } = string.Empty; public string? descripcion { get; set; } public int? cod_nivel_final { get; set; } public int cod_estado { get; set; } public string estado { get; set; } = string.Empty; public List<RutaIdiomaAsignaturaDto> asignaturas { get; set; } = []; }
public sealed class RutaIdiomaAsignaturaDto { public long cod { get; set; } public int cod_ruta_idioma { get; set; } public int cod_asignatura { get; set; } public string codigo_asignatura { get; set; } = string.Empty; public string asignatura { get; set; } = string.Empty; public int? cod_nivel_idioma { get; set; } public string? nivel_idioma { get; set; } public int orden { get; set; } public int cod_estado { get; set; } }
public sealed class CertificacionIdiomaDto { public int cod { get; set; } public int cod_idioma { get; set; } public string idioma { get; set; } = string.Empty; public string codigo { get; set; } = string.Empty; public string nombre { get; set; } = string.Empty; public string? entidad_emisora { get; set; } public string? descripcion { get; set; } public int? vigencia_meses { get; set; } public int cod_estado { get; set; } public string estado { get; set; } = string.Empty; public List<CertificacionNivelDto> niveles { get; set; } = []; }
public sealed class CertificacionNivelDto { public long cod { get; set; } public int cod_certificacion { get; set; } public int cod_nivel_idioma { get; set; } public string nivel { get; set; } = string.Empty; public decimal? puntaje_minimo { get; set; } public decimal? puntaje_maximo { get; set; } }
public sealed class ActividadCocurricularDto { public int cod { get; set; } public string codigo { get; set; } = string.Empty; public string nombre { get; set; } = string.Empty; public string? descripcion { get; set; } public int cod_tipo_actividad { get; set; } public string tipo_actividad { get; set; } = string.Empty; public int cod_estado { get; set; } public string estado { get; set; } = string.Empty; }

public sealed class EvidenciaRequisitoDto
{
    public long cod { get; set; }
    public long cod_estudiante_requisito { get; set; }
    public long? cod_requisito_mecanismo { get; set; }
    public int cod_tipo_evidencia { get; set; }
    public string tipo_evidencia { get; set; } = string.Empty;
    public string nombre { get; set; } = string.Empty;
    public string? referencia { get; set; }
    public string? resultado { get; set; }
    public DateTime? fecha_emision { get; set; }
    public DateTime? fecha_vencimiento { get; set; }
    public string? observacion { get; set; }
}
