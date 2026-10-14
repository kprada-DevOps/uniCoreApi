using UniCore.Api.Database;
using UniCore.Api.Modules.PlanEstudios.Dto;
using UniCore.Api.Modules.PlanEstudios.Requests;

namespace UniCore.Api.Modules.PlanEstudios.Managers;

public sealed class CatalogosRelacionadosManager
{
    public Task<bool> ExisteActividad(int cod, string conexion) =>
        DatabaseConnection.ExecuteScalar<bool>(conexion, "SELECT EXISTS(SELECT 1 FROM aca_actividades_cocurriculares WHERE cod=@cod);", new { cod });

    public Task<bool> ReferenciasValidas(ActividadCocurricularRequest request, string conexion) =>
        DatabaseConnection.ExecuteScalar<bool>(conexion, "SELECT EXISTS(SELECT 1 FROM aca_tipos_actividad_cocurricular t JOIN aca_estados_configuracion e ON e.cod=@estado AND e.activo=1 WHERE t.cod=@tipo AND t.activo=1);", new { tipo = request.cod_tipo_actividad, estado = request.cod_estado });

    public Task<List<ActividadCocurricularDto>> Actividades(string conexion, bool incluirInactivos) =>
        DatabaseConnection.GetMany<ActividadCocurricularDto>(conexion, @"SELECT a.cod,a.codigo,a.nombre,a.descripcion,a.cod_tipo_actividad,t.nombre AS tipo_actividad,a.cod_estado,e.nombre AS estado FROM aca_actividades_cocurriculares a JOIN aca_tipos_actividad_cocurricular t ON t.cod=a.cod_tipo_actividad JOIN aca_estados_configuracion e ON e.cod=a.cod_estado WHERE (@all=1 OR e.codigo='ACTIVO') ORDER BY a.nombre;", new { all = incluirInactivos });

    public Task<long?> ExisteCodigoActividad(string codigo, string conexion, int? omitir = null) =>
        DatabaseConnection.ExecuteScalar<long?>(conexion, "SELECT cod FROM aca_actividades_cocurriculares WHERE codigo=@codigo AND (@omitir IS NULL OR cod<>@omitir) LIMIT 1;", new { codigo, omitir });

    public async Task<int> CrearActividad(string conexion, ActividadCocurricularRequest request) =>
        checked((int)await DatabaseConnection.Insert(conexion, "aca_actividades_cocurriculares", new { codigo = Normalizar(request.codigo), nombre = request.nombre.Trim(), descripcion = Limpiar(request.descripcion), request.cod_tipo_actividad, request.cod_estado }));

    public async Task<bool> ActualizarActividad(string conexion, int cod, ActividadCocurricularRequest request) =>
        await DatabaseConnection.Update(conexion, "aca_actividades_cocurriculares", new { cod, codigo = Normalizar(request.codigo), nombre = request.nombre.Trim(), descripcion = Limpiar(request.descripcion), request.cod_tipo_actividad, request.cod_estado, updatedday = DbHelpers.GetFechaActualDatetime(conexion) });

    private static string Normalizar(string codigo) => codigo.Trim().ToUpperInvariant();
    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
