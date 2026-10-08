using UniCore.Api.Database;
using UniCore.Api.Modules.EstructuraAcademica.Dto;
using UniCore.Api.Modules.EstructuraAcademica.Requests;

namespace UniCore.Api.Modules.EstructuraAcademica.Managers;

public sealed class DepartamentosManager(DatabaseProvider db) : CatalogoAcademicoManager<DepartamentoDto, DepartamentoRequest>(db)
{
    protected override string Tabla => "aca_departamentos";
    protected override string Select => "SELECT t.cod,t.cod_facultad,t.codigo,t.nombre,t.estado,t.createdday,f.nombre AS facultad_nombre FROM aca_departamentos t JOIN aca_facultades f ON f.cod=t.cod_facultad";
    protected override string Orden => "f.nombre,t.nombre";
    protected override string FiltroActivo => "t.estado='ACTIVO'";
    protected override string ColumnaEstado => "estado";
    protected override bool ActualizarFechaModificacion => false;
    protected override object ValorEstado(bool activo) => activo ? "ACTIVO" : "INACTIVO";
    protected override object DatosActualizacion(int cod, DepartamentoRequest r, string conexion) => new { cod, r.cod_facultad, r.codigo, r.nombre, r.estado };
}
