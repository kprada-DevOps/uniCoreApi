using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniCore.Api.Helpers;
using UniCore.Api.Modules.PlanEstudios.Managers;
using UniCore.Api.Modules.PlanEstudios.Requests;
using UniCore.Api.Modules.Seguridad.Managers;

namespace UniCore.Api.Modules.PlanEstudios.Controllers;

[ApiController, Route("{conexion}/PlanEstudios/Estudiantes"), Authorize]
public sealed class CumplimientoEstudianteController : ControllerBase
{
    private readonly CumplimientoEstudianteManager _manager;
    private readonly AuditoriaManager _audit;

    public CumplimientoEstudianteController(CumplimientoEstudianteManager manager, AuditoriaManager audit)
    {
        _manager = manager;
        _audit = audit;
    }
    [HttpGet("ObtenerRequisitos/{codEstudiante:long:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.CONSULTAR")]
    public async Task<ActionResult> Listar(string conexion, long codEstudiante)
        => Respuesta.Success(await _manager.Listar(conexion, codEstudiante));

    [HttpPut("GuardarCumplimiento/{codEstudiante:long:min(1)}/{codPlanRequisito:long:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.CUMPLIMIENTO")]
    public async Task<ActionResult> Guardar(string conexion, long codEstudiante, long codPlanRequisito, [FromBody] CumplimientoRequest r)
    {
        if (r.fecha_inicio.HasValue && r.fecha_cumplimiento.HasValue && r.fecha_cumplimiento.Value.Date < r.fecha_inicio.Value.Date)
            return Respuesta.Failed<object>(mensaje: "La fecha de cumplimiento no puede ser anterior a la fecha de inicio.");
        var id = await _manager.Guardar(conexion, codEstudiante, codPlanRequisito, r);
        if (!id.HasValue) return Respuesta.Failed<object>(mensaje: "El estudiante no está asociado a un plan que contenga el requisito, o el estado no es válido.");
        await _audit.Registrar(conexion, "aca_estudiante_requisitos", id.Value.ToString(), "GUARDAR_CUMPLIMIENTO", nuevo: new { codEstudiante, codPlanRequisito, r.cod_estado_cumplimiento, r.fecha_inicio, r.fecha_cumplimiento, r.observacion });
        return Respuesta.Success(await _manager.Obtener(conexion, id.Value), "Cumplimiento actualizado.");
    }

    [HttpPost("AgregarEvidencia/{codCumplimiento:long:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.CUMPLIMIENTO")]
    public async Task<ActionResult> AgregarEvidencia(string conexion, long codCumplimiento, [FromBody] EvidenciaRequest r)
    {
        if (r.fecha_emision.HasValue && r.fecha_vencimiento.HasValue && r.fecha_vencimiento.Value.Date < r.fecha_emision.Value.Date)
            return Respuesta.Failed<object>(mensaje: "La fecha de vencimiento no puede ser anterior a la fecha de emisión.");
        var id = await _manager.AgregarEvidencia(conexion, codCumplimiento, r);
        if (!id.HasValue) return Respuesta.Failed<object>(mensaje: "El mecanismo, tipo de evidencia o requisito no es válido.");
        await _audit.Registrar(conexion, "aca_estudiante_requisito_evidencias", id.Value.ToString(), "CREAR", nuevo: r);
        return Respuesta.Success(await _manager.ObtenerEvidencia(conexion, id.Value), "Evidencia registrada.");
    }

    [HttpDelete("EliminarEvidencia/{codEvidencia:long:min(1)}"), Authorize(Policy = "PERMISO:PLAN_ESTUDIOS.CUMPLIMIENTO")]
    public async Task<ActionResult> EliminarEvidencia(string conexion, long codEvidencia)
    {
        var antes = await _manager.ObtenerEvidencia(conexion, codEvidencia); if (antes is null) return Respuesta.NotFound("La evidencia no existe.");
        await _manager.EliminarEvidencia(conexion, codEvidencia); await _audit.Registrar(conexion, "aca_estudiante_requisito_evidencias", codEvidencia.ToString(), "ELIMINAR", antes);
        return Respuesta.Success<object?>(null, "Evidencia eliminada.");
    }
}
