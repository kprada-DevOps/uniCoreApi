using UniCore.Api.Database;
using UniCore.Api.Modules.EstructuraAcademica.Dto;
using UniCore.Api.Modules.EstructuraAcademica.Requests;

namespace UniCore.Api.Modules.EstructuraAcademica.Managers;

public sealed class FacultadesManager : CatalogoAcademicoManager<FacultadDto, FacultadRequest>
{
    protected override string Tabla => "aca_facultades";
    protected override string Select => "SELECT t.cod,t.cod_sede,t.codigo,t.nombre,t.estado,t.createdday,s.nombre AS sede_nombre FROM aca_facultades t JOIN aca_sedes s ON s.cod=t.cod_sede";
    protected override string Orden => "s.nombre,t.nombre";
    protected override string FiltroActivo => "t.estado='ACTIVA'";
    protected override string ColumnaEstado => "estado";
    protected override bool ActualizarFechaModificacion => false;
    protected override object ValorEstado(bool activo) => activo ? "ACTIVA" : "INACTIVA";
    protected override object DatosActualizacion(int cod, FacultadRequest r, string conexion) => new { cod, r.cod_sede, r.codigo, r.nombre, r.estado };
}
