using UniCore.Api.Database;
using UniCore.Api.Modules.EstructuraAcademica.Dto;
using UniCore.Api.Modules.EstructuraAcademica.Requests;

namespace UniCore.Api.Modules.EstructuraAcademica.Managers;

public sealed class TipoPeriodoManager(DatabaseProvider db) : CatalogoAcademicoManager<TipoPeriodoDto, TipoPeriodoRequest>(db)
{
    protected override string Tabla => "aca_tipo_periodo";
    protected override string Select => "SELECT t.cod,t.codigo,t.nombre,t.descripcion,t.activo,t.createdday,t.updatedday FROM aca_tipo_periodo t";
    protected override string Orden => "t.nombre";
    protected override string FiltroActivo => "t.activo=1";
    protected override string ColumnaEstado => "activo";
    protected override bool ActualizarFechaModificacion => true;
    protected override object ValorEstado(bool activo) => activo;
    protected override object DatosActualizacion(int cod, TipoPeriodoRequest r, string conexion) => new { cod, r.codigo, r.nombre, r.descripcion, r.activo, updatedday = DbHelpers.GetFechaActualDatetime(conexion) };
}
