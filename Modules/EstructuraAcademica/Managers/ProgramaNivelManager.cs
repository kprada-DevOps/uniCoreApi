using UniCore.Api.Database;
using UniCore.Api.Modules.EstructuraAcademica.Dto;
using UniCore.Api.Modules.EstructuraAcademica.Requests;

namespace UniCore.Api.Modules.EstructuraAcademica.Managers;

public sealed class ProgramaNivelManager(DatabaseProvider db) : CatalogoAcademicoManager<ProgramaNivelDto, ProgramaNivelRequest>(db)
{
    protected override string Tabla => "aca_programa_nivel";
    protected override string Select => "SELECT t.cod,t.codigo,t.nombre,t.descripcion,t.activo,t.createdday,t.updatedday FROM aca_programa_nivel t";
    protected override string Orden => "t.nombre";
    protected override string FiltroActivo => "t.activo=1";
    protected override string ColumnaEstado => "activo";
    protected override bool ActualizarFechaModificacion => true;
    protected override object ValorEstado(bool activo) => activo;
    protected override object DatosActualizacion(int cod, ProgramaNivelRequest r, string conexion) => new { cod, r.codigo, r.nombre, r.descripcion, r.activo, updatedday = DbHelpers.GetFechaActualDatetime(conexion) };
}
