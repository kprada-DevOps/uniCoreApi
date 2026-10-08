using UniCore.Api.Database;
using UniCore.Api.Modules.PlanEstudios.Dto;
using UniCore.Api.Modules.PlanEstudios.Requests;

namespace UniCore.Api.Modules.PlanEstudios.Managers;

public sealed class ComponentesManager(DatabaseProvider db)
{
    private const string Select = @"SELECT c.cod,c.codigo,c.nombre,c.descripcion,c.cod_tipo_componente,t.nombre AS tipo_componente,c.cod_comportamiento,b.nombre AS comportamiento,c.creditos,c.cod_estado,e.nombre AS estado FROM aca_componentes c JOIN aca_tipos_componente t ON t.cod=c.cod_tipo_componente JOIN aca_comportamientos_componente b ON b.cod=c.cod_comportamiento JOIN aca_estados_configuracion e ON e.cod=c.cod_estado";
    public async Task<List<ComponenteDto>> Listar(string conexion, bool incluirInactivos, string? busqueda)
    {
        var list = await db.GetMany<ComponenteDto>($"{Select} WHERE (@inactivos=1 OR e.codigo='ACTIVO') AND (@filtro IS NULL OR c.codigo LIKE @filtro OR c.nombre LIKE @filtro) ORDER BY c.nombre,c.codigo;", new { inactivos = incluirInactivos, filtro = string.IsNullOrWhiteSpace(busqueda) ? null : $"%{busqueda.Trim()}%" }, conexion);
        await CargarOpciones(conexion, list);
        return list;
    }
    public async Task<ComponenteDto?> Obtener(string conexion, int cod)
    {
        var item = await db.GetOne<ComponenteDto>($"{Select} WHERE c.cod=@cod LIMIT 1;", new { cod }, conexion);
        if (item is not null) await CargarOpciones(conexion, [item]);
        return item;
    }
    public Task<long?> ExisteCodigo(string conexion, string codigo, int? omitir) => db.ExecuteScalar<long?>("SELECT cod FROM aca_componentes WHERE codigo=@codigo AND (@omitir IS NULL OR cod<>@omitir) LIMIT 1;", new { codigo, omitir }, conexion);
    public Task<bool> ReferenciasValidas(string conexion, ComponenteRequest r) => db.ExecuteScalar<bool>("SELECT EXISTS(SELECT 1 FROM aca_tipos_componente t JOIN aca_comportamientos_componente b ON b.cod=@comportamiento AND b.activo=1 JOIN aca_estados_configuracion e ON e.cod=@estado AND e.activo=1 WHERE t.cod=@tipo AND t.activo=1);", new { tipo = r.cod_tipo_componente, comportamiento = r.cod_comportamiento, estado = r.cod_estado }, conexion);
    public Task<bool> EstadoExiste(string conexion, int estado) => db.ExecuteScalar<bool>("SELECT EXISTS(SELECT 1 FROM aca_estados_configuracion WHERE cod=@estado AND activo=1);", new { estado }, conexion);
    public async Task<int> Crear(string conexion, ComponenteRequest r)
    {
        var id = await db.Insert("aca_componentes", Param(r), conexion); return checked((int)id);
    }
    public async Task<bool> Actualizar(string conexion, int cod, ComponenteRequest r)
        => await db.Execute("UPDATE aca_componentes SET codigo=@codigo,nombre=@nombre,descripcion=@descripcion,cod_tipo_componente=@cod_tipo_componente,cod_comportamiento=@cod_comportamiento,creditos=@creditos,cod_estado=@cod_estado,updatedday=NOW() WHERE cod=@cod;", new { cod, codigo = r.codigo.Trim().ToUpperInvariant(), nombre = r.nombre.Trim(), descripcion = Limpio(r.descripcion), r.cod_tipo_componente, r.cod_comportamiento, r.creditos, r.cod_estado }, conexion) > 0;
    public async Task<bool> CambiarEstado(string conexion, int cod, int estado)
        => await db.Execute("UPDATE aca_componentes SET cod_estado=@estado,updatedday=NOW() WHERE cod=@cod;", new { cod, estado }, conexion) > 0 || await Obtener(conexion, cod) is not null;
    public async Task<bool> ReemplazarOpciones(string conexion, int cod, IReadOnlyCollection<ComponenteOpcionRequest> opciones)
    {
        var ids = opciones.Select(o => o.cod_asignatura).ToArray();
        if (ids.Distinct().Count() != ids.Length) throw new InvalidOperationException("No se permiten asignaturas repetidas como opciones del componente.");
        await using var tx = db.BeginTransaction(conexion);
        if (ids.Length > 0 && (await db.GetManyTransaccion<int>(tx, "SELECT cod FROM aca_asignaturas WHERE cod IN @ids;", new { ids })).Count != ids.Length)
            throw new InvalidOperationException("Una asignatura indicada no existe.");
        var estados = opciones.Select(o => o.cod_estado).Distinct().ToArray();
        if (estados.Length > 0 && (await db.GetManyTransaccion<int>(tx, "SELECT cod FROM aca_estados_configuracion WHERE cod IN @ids AND activo=1;", new { ids = estados })).Count != estados.Length)
            throw new InvalidOperationException("Un estado de opción no existe o está inactivo.");
        await db.ExecuteTransaccion(tx, "DELETE FROM aca_componente_opciones WHERE cod_componente=@cod;", new { cod });
        foreach (var opcion in opciones)
            await db.ExecuteTransaccion(tx, "INSERT INTO aca_componente_opciones(cod_componente,cod_asignatura,orden,cod_estado) VALUES(@cod,@asignatura,@orden,@estado);", new { cod, asignatura = opcion.cod_asignatura, opcion.orden, estado = opcion.cod_estado });
        tx.Commit(); return true;
    }
    private async Task CargarOpciones(string conexion, List<ComponenteDto> componentes)
    {
        var ids = componentes.Select(c => c.cod).ToArray();
        if (ids.Length == 0) return;
        var opciones = await db.GetMany<ComponenteOpcionDto>(@"SELECT o.cod,o.cod_componente,o.cod_asignatura,a.codigo AS codigo_asignatura,a.nombre AS asignatura,o.orden,o.cod_estado,e.nombre AS estado FROM aca_componente_opciones o JOIN aca_asignaturas a ON a.cod=o.cod_asignatura JOIN aca_estados_configuracion e ON e.cod=o.cod_estado WHERE o.cod_componente IN @ids ORDER BY o.cod_componente,o.orden,a.nombre;", new { ids }, conexion);
        foreach (var componente in componentes) componente.opciones = opciones.Where(o => o.cod_componente == componente.cod).ToList();
    }
    private static object Param(ComponenteRequest r) => new { codigo = r.codigo.Trim().ToUpperInvariant(), nombre = r.nombre.Trim(), descripcion = Limpio(r.descripcion), r.cod_tipo_componente, r.cod_comportamiento, r.creditos, r.cod_estado };
    private static string? Limpio(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
