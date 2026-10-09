using UniCore.Api.Database;
using UniCore.Api.Modules.EstructuraAcademica.Dto;
using UniCore.Api.Modules.EstructuraAcademica.Requests;

namespace UniCore.Api.Modules.EstructuraAcademica.Managers;

public sealed class ProgramasManager : CatalogoAcademicoManager<ProgramaDto, ProgramaRequest>
{
    protected override string Tabla => "aca_programas";
    protected override string Select => "SELECT t.cod,t.cod_facultad,t.codigo,t.nombre,t.cod_nivel,t.cod_modalidad,t.cod_tipo_periodo,t.duracion_periodos,t.registro_calificado,t.estado,t.createdday,t.updatedday,f.nombre AS facultad_nombre,n.nombre AS nivel_nombre,m.nombre AS modalidad_nombre,p.nombre AS tipo_periodo_nombre FROM aca_programas t JOIN aca_facultades f ON f.cod=t.cod_facultad JOIN aca_programa_nivel n ON n.cod=t.cod_nivel JOIN aca_programa_modalidad m ON m.cod=t.cod_modalidad JOIN aca_tipo_periodo p ON p.cod=t.cod_tipo_periodo";
    protected override string Orden => "f.nombre,t.nombre";
    protected override string FiltroActivo => "t.estado='ACTIVO'";
    protected override string ColumnaEstado => "estado";
    protected override bool ActualizarFechaModificacion => true;
    protected override object ValorEstado(bool activo) => activo ? "ACTIVO" : "INACTIVO";
    protected override object DatosActualizacion(int cod, ProgramaRequest r, string conexion) => new { cod, r.cod_facultad, r.codigo, r.nombre, r.cod_nivel, r.cod_modalidad, r.cod_tipo_periodo, r.duracion_periodos, r.registro_calificado, r.estado, updatedday = DbHelpers.GetFechaActualDatetime(conexion) };
}
