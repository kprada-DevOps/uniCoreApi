using UniCore.Api.Database;
using UniCore.Api.Modules.PlanEstudios.Dto;
using UniCore.Api.Modules.PlanEstudios.Requests;

namespace UniCore.Api.Modules.PlanEstudios.Managers;

public sealed class CumplimientoEstudianteManager
{
    public async Task<List<CumplimientoEstudianteDto>> Listar(string conexion, long estudiante)
    {
        var rows = await DatabaseConnection.GetMany<CumplimientoEstudianteDto>(conexion, @"SELECT COALESCE(er.cod,0) AS cod,ep.cod_estudiante,est.codigo_estudiante,pr.cod AS cod_plan_requisito,pl.cod AS cod_plan_estudio,pl.nombre AS plan_estudio,pr.cod_requisito,r.nombre AS requisito,COALESCE(er.cod_estado_cumplimiento,(SELECT cod FROM aca_estados_cumplimiento WHERE codigo='PENDIENTE' LIMIT 1)) AS cod_estado_cumplimiento,COALESCE(ec.nombre,'Pendiente') AS estado_cumplimiento,er.fecha_inicio,er.fecha_cumplimiento,er.observacion FROM aca_estudiante_programas ep JOIN per_estudiantes est ON est.cod=ep.cod_estudiante JOIN aca_planes_estudio pl ON pl.cod=ep.cod_plan_estudio JOIN aca_plan_requisitos pr ON pr.cod_plan_estudio=pl.cod JOIN aca_estados_configuracion pr_estado ON pr_estado.cod=pr.cod_estado AND pr_estado.codigo='ACTIVO' JOIN aca_requisitos_cocurriculares r ON r.cod=pr.cod_requisito LEFT JOIN aca_estudiante_requisitos er ON er.cod_estudiante=ep.cod_estudiante AND er.cod_plan_requisito=pr.cod LEFT JOIN aca_estados_cumplimiento ec ON ec.cod=er.cod_estado_cumplimiento WHERE ep.cod_estudiante=@estudiante AND ep.estado='ACTIVO' ORDER BY pl.nombre,r.nombre;", new { estudiante });
        var ids = rows.Where(r => r.cod > 0).Select(r => r.cod).ToArray();
        if (ids.Length == 0) return rows;
        var evidencias = await DatabaseConnection.GetMany<EvidenciaRequisitoDto>(conexion, @"SELECT ev.cod,ev.cod_estudiante_requisito,ev.cod_requisito_mecanismo,ev.cod_tipo_evidencia,t.nombre AS tipo_evidencia,ev.nombre,ev.referencia,ev.resultado,ev.fecha_emision,ev.fecha_vencimiento,ev.observacion FROM aca_estudiante_requisito_evidencias ev JOIN aca_tipos_evidencia t ON t.cod=ev.cod_tipo_evidencia WHERE ev.cod_estudiante_requisito IN @ids ORDER BY ev.createdday DESC,ev.cod DESC;", new { ids });
        foreach (var row in rows) row.evidencias = evidencias.Where(e => e.cod_estudiante_requisito == row.cod).ToList();
        return rows;
    }

    public Task<CumplimientoEstudianteDto?> Obtener(string conexion, long cod)
        => DatabaseConnection.GetOne<CumplimientoEstudianteDto>(conexion, @"SELECT er.cod,er.cod_estudiante,est.codigo_estudiante,er.cod_plan_requisito,pl.cod AS cod_plan_estudio,pl.nombre AS plan_estudio,pr.cod_requisito,r.nombre AS requisito,er.cod_estado_cumplimiento,ec.nombre AS estado_cumplimiento,er.fecha_inicio,er.fecha_cumplimiento,er.observacion FROM aca_estudiante_requisitos er JOIN per_estudiantes est ON est.cod=er.cod_estudiante JOIN aca_plan_requisitos pr ON pr.cod=er.cod_plan_requisito JOIN aca_planes_estudio pl ON pl.cod=pr.cod_plan_estudio JOIN aca_requisitos_cocurriculares r ON r.cod=pr.cod_requisito JOIN aca_estados_cumplimiento ec ON ec.cod=er.cod_estado_cumplimiento WHERE er.cod=@cod LIMIT 1;", new { cod });

    public async Task<long?> Guardar(string conexion, long estudiante, long planRequisito, CumplimientoRequest r)
    {
        var valido = await DatabaseConnection.ExecuteScalar<bool>(conexion, @"SELECT EXISTS(SELECT 1 FROM aca_estudiante_programas ep JOIN aca_plan_requisitos pr ON pr.cod_plan_estudio=ep.cod_plan_estudio WHERE ep.cod_estudiante=@estudiante AND ep.estado='ACTIVO' AND pr.cod=@planRequisito);", new { estudiante, planRequisito });
        if (!valido) return null;
        var estadoExiste = await DatabaseConnection.ExecuteScalar<bool>(conexion, "SELECT EXISTS(SELECT 1 FROM aca_estados_cumplimiento WHERE cod=@estado AND activo=1);", new { estado = r.cod_estado_cumplimiento });
        if (!estadoExiste) return null;
        await DatabaseConnection.Execute(conexion, @"INSERT INTO aca_estudiante_requisitos(cod_estudiante,cod_plan_requisito,cod_estado_cumplimiento,fecha_inicio,fecha_cumplimiento,observacion) VALUES(@estudiante,@planRequisito,@estado,@inicio,@cumplimiento,@observacion) ON DUPLICATE KEY UPDATE cod_estado_cumplimiento=VALUES(cod_estado_cumplimiento),fecha_inicio=VALUES(fecha_inicio),fecha_cumplimiento=VALUES(fecha_cumplimiento),observacion=VALUES(observacion),updatedday=NOW();", new { estudiante, planRequisito, estado = r.cod_estado_cumplimiento, inicio = r.fecha_inicio?.Date, cumplimiento = r.fecha_cumplimiento?.Date, observacion = Limpio(r.observacion) });
        return await DatabaseConnection.ExecuteScalar<long>(conexion, "SELECT cod FROM aca_estudiante_requisitos WHERE cod_estudiante=@estudiante AND cod_plan_requisito=@planRequisito LIMIT 1;", new { estudiante, planRequisito });
    }

    public async Task<long?> AgregarEvidencia(string conexion, long cumplimiento, EvidenciaRequest r)
    {
        var requisito = await DatabaseConnection.ExecuteScalar<int?>(conexion, "SELECT pr.cod_requisito FROM aca_estudiante_requisitos er JOIN aca_plan_requisitos pr ON pr.cod=er.cod_plan_requisito WHERE er.cod=@cumplimiento;", new { cumplimiento });
        if (!requisito.HasValue) return null;
        if (r.cod_requisito_mecanismo.HasValue && !await DatabaseConnection.ExecuteScalar<bool>(conexion, "SELECT EXISTS(SELECT 1 FROM aca_requisito_mecanismos WHERE cod=@mecanismo AND cod_requisito=@requisito AND cod_estado IN (SELECT cod FROM aca_estados_configuracion WHERE activo=1));", new { mecanismo = r.cod_requisito_mecanismo, requisito })) return null;
        if (!await DatabaseConnection.ExecuteScalar<bool>(conexion, "SELECT EXISTS(SELECT 1 FROM aca_tipos_evidencia WHERE cod=@cod AND activo=1);", new { cod = r.cod_tipo_evidencia })) return null;
        return await DatabaseConnection.Insert(conexion, "aca_estudiante_requisito_evidencias", new { cod_estudiante_requisito = cumplimiento, r.cod_requisito_mecanismo, r.cod_tipo_evidencia, nombre = r.nombre.Trim(), referencia = Limpio(r.referencia), resultado = Limpio(r.resultado), fecha_emision = r.fecha_emision?.Date, fecha_vencimiento = r.fecha_vencimiento?.Date, observacion = Limpio(r.observacion) });
    }

    public async Task<EvidenciaRequisitoDto?> ObtenerEvidencia(string conexion, long cod)
        => await DatabaseConnection.GetOne<EvidenciaRequisitoDto>(conexion, @"SELECT ev.cod,ev.cod_estudiante_requisito,ev.cod_requisito_mecanismo,ev.cod_tipo_evidencia,t.nombre AS tipo_evidencia,ev.nombre,ev.referencia,ev.resultado,ev.fecha_emision,ev.fecha_vencimiento,ev.observacion FROM aca_estudiante_requisito_evidencias ev JOIN aca_tipos_evidencia t ON t.cod=ev.cod_tipo_evidencia WHERE ev.cod=@cod LIMIT 1;", new { cod });

    public async Task<bool> EliminarEvidencia(string conexion, long cod) => await DatabaseConnection.Execute(conexion, "DELETE FROM aca_estudiante_requisito_evidencias WHERE cod=@cod;", new { cod }) > 0;
    private static string? Limpio(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
