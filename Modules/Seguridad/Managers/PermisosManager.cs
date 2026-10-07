using UniCore.Api.Database;
using UniCore.Api.Modules.Seguridad.Dto;
using UniCore.Api.Modules.Seguridad.Requests;

namespace UniCore.Api.Modules.Seguridad.Managers;

public sealed class PermisosManager(DatabaseProvider database)
{
    private readonly DatabaseProvider _db = database;
    private const string Select = "SELECT cod, cod_modulo, codigo, nombre, accion, activo, descripcion, createdday FROM seg_permisos";

    public Task<List<PermisoSeguridadDto>> Listar(string conexion, int? modulo, bool incluirInactivos, string? busqueda)
        => _db.GetMany<PermisoSeguridadDto>($"{Select} WHERE (@modulo IS NULL OR cod_modulo=@modulo) AND (@inactivos=1 OR activo=1) AND (@busqueda IS NULL OR codigo LIKE @busqueda OR nombre LIKE @busqueda) ORDER BY cod_modulo,codigo;",
            new { modulo, inactivos = incluirInactivos, busqueda = string.IsNullOrWhiteSpace(busqueda) ? null : $"%{busqueda.Trim()}%" }, conexion);

    public Task<PermisoSeguridadDto?> Obtener(int cod, string conexion)
        => _db.GetOne<PermisoSeguridadDto>($"{Select} WHERE cod=@cod LIMIT 1;", new { cod }, conexion);

    public Task<long?> ExisteCodigo(string codigo, int? omitir, string conexion)
        => _db.ExecuteScalar<long?>("SELECT cod FROM seg_permisos WHERE codigo=@codigo AND (@omitir IS NULL OR cod<>@omitir) LIMIT 1;", new { codigo, omitir }, conexion);

    public async Task<bool> ModuloExiste(int cod, string conexion)
        => await _db.ExecuteScalar<long>("SELECT COUNT(*) FROM seg_modulos WHERE cod=@cod;", new { cod }, conexion) > 0;

    public async Task<int> Crear(PermisoRequest r, string conexion)
    {
        await using var tx = _db.BeginTransaction(conexion);
        const string sql = "INSERT INTO seg_permisos(cod_modulo,codigo,nombre,accion,activo,descripcion) VALUES(@cod_modulo,@codigo,@nombre,@accion,@activo,@descripcion);";
        await _db.ExecuteTransaccion(tx, sql, Param(r));
        var id = await _db.ExecuteScalarTransaccion<int>(tx, "SELECT LAST_INSERT_ID();");
        tx.Commit(); return id;
    }

    public async Task<bool> Actualizar(int cod, PermisoRequest r, string conexion)
        => await _db.Execute("UPDATE seg_permisos SET cod_modulo=@cod_modulo,codigo=@codigo,nombre=@nombre,accion=@accion,activo=@activo,descripcion=@descripcion WHERE cod=@cod;", Param(r, cod), conexion) > 0;

    public async Task<bool> Estado(int cod, bool activo, string conexion)
    {
        var n = await _db.Execute("UPDATE seg_permisos SET activo=@activo WHERE cod=@cod;", new { cod, activo }, conexion);
        return n > 0 || await Obtener(cod, conexion) is not null;
    }

    private static object Param(PermisoRequest r, int? cod = null) => new { cod, r.cod_modulo, codigo = r.codigo.Trim().ToUpperInvariant(), nombre = r.nombre.Trim(), accion = r.accion.ToUpperInvariant(), r.activo, descripcion = string.IsNullOrWhiteSpace(r.descripcion) ? null : r.descripcion.Trim() };
}
