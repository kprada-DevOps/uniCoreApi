using UniCore.Api.Database;
using UniCore.Api.Modules.EstructuraAcademica.Dto;
using UniCore.Api.Modules.EstructuraAcademica.Requests;

namespace UniCore.Api.Modules.EstructuraAcademica.Managers;

public sealed class SedesManager : CatalogoAcademicoManager<SedeDto, SedeRequest>
{
    protected override string Tabla => "aca_sedes";
    protected override string Select => "SELECT t.cod,t.codigo,t.nombre,t.direccion,t.ciudad,t.departamento,t.pais,t.estado,t.createdday FROM aca_sedes t";
    protected override string Orden => "t.nombre";
    protected override string FiltroActivo => "t.estado='ACTIVA'";
    protected override string ColumnaEstado => "estado";
    protected override bool ActualizarFechaModificacion => false;
    protected override object ValorEstado(bool activo) => activo ? "ACTIVA" : "INACTIVA";
    protected override object DatosActualizacion(int cod, SedeRequest r, string conexion) => new { cod, r.codigo, r.nombre, r.direccion, r.ciudad, r.departamento, r.pais, r.estado };
}
