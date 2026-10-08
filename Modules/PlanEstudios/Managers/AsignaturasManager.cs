using UniCore.Api.Database;
using UniCore.Api.Modules.PlanEstudios.Dto;
using UniCore.Api.Modules.PlanEstudios.Requests;

namespace UniCore.Api.Modules.PlanEstudios.Managers;

public sealed class AsignaturasManager(DatabaseProvider db)
{
    private const string Select = @"SELECT a.cod,a.codigo,a.nombre,a.descripcion,a.creditos,a.horas_teoricas,a.horas_practicas,a.cod_tipo_asignatura,t.nombre AS tipo_asignatura,a.cod_estado,e.nombre AS estado,a.createdday,a.updatedday FROM aca_asignaturas a JOIN aca_tipos_asignatura t ON t.cod=a.cod_tipo_asignatura JOIN aca_estados_configuracion e ON e.cod=a.cod_estado";
    public Task<List<AsignaturaDto>> Listar(string conexion, bool incluirInactivas, string? busqueda)
        => db.GetMany<AsignaturaDto>($"{Select} WHERE (@inactivos=1 OR e.codigo='ACTIVO') AND (@busqueda IS NULL OR a.codigo LIKE @filtro OR a.nombre LIKE @filtro) ORDER BY a.nombre,a.codigo;", new { inactivos = incluirInactivas, busqueda, filtro = string.IsNullOrWhiteSpace(busqueda) ? null : $"%{busqueda.Trim()}%" }, conexion);
    public Task<AsignaturaDto?> Obtener(string conexion, int cod) => db.GetOne<AsignaturaDto>($"{Select} WHERE a.cod=@cod LIMIT 1;", new { cod }, conexion);
    public Task<long?> ExisteCodigo(string conexion, string codigo, int? omitir) => db.ExecuteScalar<long?>("SELECT cod FROM aca_asignaturas WHERE codigo=@codigo AND (@omitir IS NULL OR cod<>@omitir) LIMIT 1;", new { codigo, omitir }, conexion);
    public Task<bool> TipoExiste(string conexion, int cod) => db.ExecuteScalar<bool>("SELECT EXISTS(SELECT 1 FROM aca_tipos_asignatura WHERE cod=@cod AND activo=1);", new { cod }, conexion);
    public Task<bool> EstadoExiste(string conexion, int cod) => db.ExecuteScalar<bool>("SELECT EXISTS(SELECT 1 FROM aca_estados_configuracion WHERE cod=@cod AND activo=1);", new { cod }, conexion);
    public async Task<int> Crear(string conexion, AsignaturaRequest r)
    {
        var id = await db.Insert("aca_asignaturas", Param(r), conexion);
        return checked((int)id);
    }
    public async Task<bool> Actualizar(string conexion, int cod, AsignaturaRequest r)
        => await db.Execute("UPDATE aca_asignaturas SET codigo=@codigo,nombre=@nombre,descripcion=@descripcion,creditos=@creditos,horas_teoricas=@horas_teoricas,horas_practicas=@horas_practicas,cod_tipo_asignatura=@cod_tipo_asignatura,cod_estado=@cod_estado,updatedday=NOW() WHERE cod=@cod;", new { cod, codigo = r.codigo.Trim().ToUpperInvariant(), nombre = r.nombre.Trim(), descripcion = Limpio(r.descripcion), r.creditos, r.horas_teoricas, r.horas_practicas, r.cod_tipo_asignatura, r.cod_estado }, conexion) > 0;
    public async Task<bool> CambiarEstado(string conexion, int cod, int estado)
        => await db.Execute("UPDATE aca_asignaturas SET cod_estado=@estado,updatedday=NOW() WHERE cod=@cod;", new { cod, estado }, conexion) > 0 || await Obtener(conexion, cod) is not null;
    public Task<List<PrerrequisitoDto>> Prerrequisitos(string conexion, int asignatura)
        => db.GetMany<PrerrequisitoDto>(@"SELECT p.cod_asignatura,a.codigo AS codigo_asignatura,a.nombre AS nombre_asignatura,p.cod_asignatura_requisito,r.codigo AS codigo_requisito,r.nombre AS nombre_requisito,p.cod_tipo_prerrequisito,t.nombre AS tipo_prerrequisito FROM aca_asignatura_prerrequisitos p JOIN aca_asignaturas a ON a.cod=p.cod_asignatura JOIN aca_asignaturas r ON r.cod=p.cod_asignatura_requisito JOIN aca_tipos_prerrequisito t ON t.cod=p.cod_tipo_prerrequisito WHERE p.cod_asignatura=@asignatura ORDER BY r.nombre;", new { asignatura }, conexion);
    public async Task<bool> AgregarPrerrequisito(string conexion, int asignatura, PrerrequisitoRequest r)
    {
        if (asignatura == r.cod_asignatura_requisito) throw new InvalidOperationException("Una asignatura no puede ser su propio prerrequisito.");
        await using var tx = db.BeginTransaction(conexion);
        var asignaturas = await db.GetManyTransaccion<int>(tx, "SELECT cod FROM aca_asignaturas WHERE cod IN (@asignatura,@requisito);", new { asignatura, requisito = r.cod_asignatura_requisito });
        if (asignaturas.Count != 2) return false;
        if (!await db.ExecuteScalarTransaccion<bool>(tx, "SELECT EXISTS(SELECT 1 FROM aca_tipos_prerrequisito WHERE cod=@tipo AND activo=1);", new { tipo = r.cod_tipo_prerrequisito })) return false;
        var duplicate = await db.ExecuteScalarTransaccion<bool>(tx, "SELECT EXISTS(SELECT 1 FROM aca_asignatura_prerrequisitos WHERE cod_asignatura=@asignatura AND cod_asignatura_requisito=@requisito);", new { asignatura, requisito = r.cod_asignatura_requisito });
        if (duplicate) return false;
        var edges = await db.GetManyTransaccion<PrerequisiteEdge>(tx, "SELECT cod_asignatura,cod_asignatura_requisito FROM aca_asignatura_prerrequisitos;");
        var graph = edges.GroupBy(x => x.cod_asignatura).ToDictionary(g => g.Key, g => g.Select(x => x.cod_asignatura_requisito).ToArray());
        var pending = new Stack<int>(); var visited = new HashSet<int>(); pending.Push(r.cod_asignatura_requisito);
        while (pending.TryPop(out var current))
        {
            if (current == asignatura) throw new InvalidOperationException("La relación crearía un ciclo de prerrequisitos.");
            if (!visited.Add(current) || !graph.TryGetValue(current, out var next)) continue;
            foreach (var cod in next) pending.Push(cod);
        }
        await db.ExecuteTransaccion(tx, "INSERT INTO aca_asignatura_prerrequisitos(cod_asignatura,cod_asignatura_requisito,cod_tipo_prerrequisito) VALUES(@asignatura,@requisito,@tipo);", new { asignatura, requisito = r.cod_asignatura_requisito, tipo = r.cod_tipo_prerrequisito });
        tx.Commit(); return true;
    }
    public async Task<bool> QuitarPrerrequisito(string conexion, int asignatura, int requisito)
        => await db.Execute("DELETE FROM aca_asignatura_prerrequisitos WHERE cod_asignatura=@asignatura AND cod_asignatura_requisito=@requisito;", new { asignatura, requisito }, conexion) > 0;
    private static object Param(AsignaturaRequest r) => new { codigo = r.codigo.Trim().ToUpperInvariant(), nombre = r.nombre.Trim(), descripcion = Limpio(r.descripcion), r.creditos, r.horas_teoricas, r.horas_practicas, r.cod_tipo_asignatura, r.cod_estado };
    private static string? Limpio(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private sealed class PrerequisiteEdge { public int cod_asignatura { get; set; } public int cod_asignatura_requisito { get; set; } }
}
