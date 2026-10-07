using UniCore.Api.Database;
using UniCore.Api.Modules.Seguridad.Dto;
using UniCore.Api.Modules.Seguridad.Requests;

namespace UniCore.Api.Modules.Seguridad.Managers;

public sealed class MenuManager(DatabaseProvider db)
{
    private readonly DatabaseProvider _db = db;
    private const string Select = "SELECT cod,cod_modulo,cod_padre,codigo,nombre,descripcion,icono,ruta,orden,tipo,activo,createdday,updatedday FROM seg_menu";
    public Task<List<MenuDto>> Listar(string conexion, int? modulo, bool incluirInactivos)
        => _db.GetMany<MenuDto>($"{Select} WHERE (@modulo IS NULL OR cod_modulo=@modulo) AND (@inactivos=1 OR activo=1) ORDER BY cod_modulo,orden,nombre;", new { modulo, inactivos = incluirInactivos }, conexion);
    public Task<MenuDto?> Obtener(int cod, string conexion)
        => _db.GetOne<MenuDto>($"{Select} WHERE cod=@cod LIMIT 1;", new { cod }, conexion);
    public async Task<List<MenuDto>> ParaUsuario(long usuario, string conexion)
    {
        const string sql = @"
SELECT m.cod,m.cod_modulo,m.cod_padre,m.codigo,m.nombre,m.descripcion,m.icono,m.ruta,m.orden,m.tipo,m.activo,m.createdday,m.updatedday
FROM seg_menu m
INNER JOIN seg_modulos mo ON mo.cod=m.cod_modulo AND mo.activo=1
LEFT JOIN seg_modulos_configuracion mc ON mc.cod_modulo=mo.cod
WHERE m.activo=1 AND EXISTS (
 SELECT 1 FROM seg_usuario_roles ur JOIN seg_roles r ON r.cod=ur.cod_rol AND r.activo=1
 JOIN seg_rol_permisos rp ON rp.cod_rol=r.cod JOIN seg_permisos p ON p.cod=rp.cod_permiso AND p.activo=1
 WHERE ur.cod_usuario=@usuario AND p.cod_modulo=m.cod_modulo
 AND (mc.cod IS NULL OR (mc.habilitado=1 AND (mc.fecha_inicio IS NULL OR mc.fecha_inicio<=NOW()) AND (mc.fecha_fin IS NULL OR mc.fecha_fin>=NOW()))))
ORDER BY m.cod_modulo,m.cod_padre,m.orden,m.nombre;";
        var list = await _db.GetMany<MenuDto>(sql, new { usuario }, conexion);
        var byId = list.ToDictionary(i => i.cod);
        var roots = new List<MenuDto>();
        foreach (var item in list)
        {
            if (!item.cod_padre.HasValue) roots.Add(item);
            else if (byId.TryGetValue(item.cod_padre.Value, out var parent)) parent.hijos.Add(item);
        }
        return roots;
    }
    public Task<long?> CodigoExiste(string codigo, int? omitir, string conexion)
        => _db.ExecuteScalar<long?>("SELECT cod FROM seg_menu WHERE codigo=@codigo AND (@omitir IS NULL OR cod<>@omitir) LIMIT 1;", new { codigo, omitir }, conexion);
    public async Task<bool> ModuloExiste(int cod, string conexion)
        => await _db.ExecuteScalar<long>("SELECT COUNT(*) FROM seg_modulos WHERE cod=@cod AND activo=1;", new { cod }, conexion) > 0;
    public async Task<bool> PadreValido(int? padre, int modulo, int? omitir, string conexion)
    {
        if (!padre.HasValue) return true;
        if (omitir == padre) return false;
        const string sql = @"WITH RECURSIVE descendientes AS (
 SELECT cod FROM seg_menu WHERE cod=@omitir
 UNION ALL SELECT m.cod FROM seg_menu m JOIN descendientes d ON m.cod_padre=d.cod
)
SELECT COUNT(*) FROM seg_menu WHERE cod=@padre AND cod_modulo=@modulo AND activo=1
 AND (@omitir IS NULL OR cod NOT IN (SELECT cod FROM descendientes));";
        return await _db.ExecuteScalar<long>(sql, new { padre, modulo, omitir }, conexion) > 0;
    }
    public async Task<int> Crear(MenuRequest r, string conexion)
    {
        await using var tx = _db.BeginTransaction(conexion);
        const string sql = "INSERT INTO seg_menu(cod_modulo,cod_padre,codigo,nombre,descripcion,icono,ruta,orden,tipo,activo) VALUES(@cod_modulo,@cod_padre,@codigo,@nombre,@descripcion,@icono,@ruta,@orden,@tipo,@activo);";
        await _db.ExecuteTransaccion(tx, sql, P(r));
        var id = await _db.ExecuteScalarTransaccion<int>(tx, "SELECT LAST_INSERT_ID();");
        tx.Commit(); return id;
    }
    public async Task<bool> Actualizar(int cod, MenuRequest r, string conexion)
        => await _db.Execute("UPDATE seg_menu SET cod_modulo=@cod_modulo,cod_padre=@cod_padre,codigo=@codigo,nombre=@nombre,descripcion=@descripcion,icono=@icono,ruta=@ruta,orden=@orden,tipo=@tipo,activo=@activo,updatedday=NOW() WHERE cod=@cod;", P(r,cod), conexion) > 0;
    public async Task<bool> Estado(int cod, bool activo, string conexion)
    {
        var n = await _db.Execute("UPDATE seg_menu SET activo=@activo,updatedday=NOW() WHERE cod=@cod;", new { cod, activo }, conexion);
        return n > 0 || await _db.ExecuteScalar<long>("SELECT COUNT(*) FROM seg_menu WHERE cod=@cod;", new { cod }, conexion) > 0;
    }
    private static object P(MenuRequest r, int? cod = null) => new { cod, r.cod_modulo, r.cod_padre, codigo=r.codigo.Trim().ToUpperInvariant(), nombre=r.nombre.Trim(), descripcion=Clean(r.descripcion), icono=Clean(r.icono), ruta=Clean(r.ruta), r.orden, tipo=r.tipo.Trim().ToUpperInvariant(), r.activo };
    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
