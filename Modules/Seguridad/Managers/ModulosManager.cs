using UniCore.Api.Database;
using UniCore.Api.Modules.Seguridad.Dto;
using UniCore.Api.Modules.Seguridad.Requests;

namespace UniCore.Api.Modules.Seguridad.Managers;

public sealed class ModulosManager(DatabaseProvider db)
{
    private readonly DatabaseProvider _db = db;
    private const string Select = @"SELECT m.cod,m.cod AS cod_modulo,m.codigo,m.nombre,m.descripcion,m.icono,m.orden,m.activo,m.createdday,m.updatedday,
COALESCE(c.habilitado,1) AS habilitado,c.fecha_inicio,c.fecha_fin FROM seg_modulos m LEFT JOIN seg_modulos_configuracion c ON c.cod_modulo=m.cod";

    public Task<List<ModuloConfiguracionDto>> Listar(string conexion, bool incluirInactivos)
        => _db.GetMany<ModuloConfiguracionDto>($"{Select} WHERE (@inactivos=1 OR m.activo=1) ORDER BY m.orden,m.nombre;", new { inactivos = incluirInactivos }, conexion);
    public Task<ModuloConfiguracionDto?> Obtener(int cod, string conexion)
        => _db.GetOne<ModuloConfiguracionDto>($"{Select} WHERE m.cod=@cod LIMIT 1;", new { cod }, conexion);
    public Task<long?> CodigoExiste(string codigo, int? omitir, string conexion)
        => _db.ExecuteScalar<long?>("SELECT cod FROM seg_modulos WHERE codigo=@codigo AND (@omitir IS NULL OR cod<>@omitir) LIMIT 1;", new { codigo, omitir }, conexion);

    public async Task<int> Crear(ModuloRequest r, string conexion)
    {
        await using var tx = _db.BeginTransaction(conexion);
        await _db.ExecuteTransaccion(tx, "INSERT INTO seg_modulos(codigo,nombre,descripcion,icono,orden,activo) VALUES(@codigo,@nombre,@descripcion,@icono,@orden,@activo);", new { codigo = r.codigo.Trim().ToUpperInvariant(), nombre = r.nombre.Trim(), descripcion = Clean(r.descripcion), icono = Clean(r.icono), r.orden, r.activo });
        var id = await _db.ExecuteScalarTransaccion<int>(tx, "SELECT LAST_INSERT_ID();");
        await _db.ExecuteTransaccion(tx, "INSERT INTO seg_modulos_configuracion(cod_modulo,habilitado,fecha_inicio,fecha_fin) VALUES(@id,@habilitado,@inicio,@fin) ON DUPLICATE KEY UPDATE habilitado=VALUES(habilitado),fecha_inicio=VALUES(fecha_inicio),fecha_fin=VALUES(fecha_fin),updatedday=NOW();", new { id, habilitado = r.habilitado, inicio = r.fecha_inicio, fin = r.fecha_fin });
        tx.Commit(); return id;
    }
    public async Task<bool> Actualizar(int cod, ModuloRequest r, string conexion)
    {
        await using var tx = _db.BeginTransaction(conexion);
        if (await _db.ExecuteScalarTransaccion<long>(tx, "SELECT COUNT(*) FROM seg_modulos WHERE cod=@cod;", new { cod }) == 0) return false;
        await _db.ExecuteTransaccion(tx, "UPDATE seg_modulos SET codigo=@codigo,nombre=@nombre,descripcion=@descripcion,icono=@icono,orden=@orden,activo=@activo,updatedday=NOW() WHERE cod=@cod;", new { cod, codigo = r.codigo.Trim().ToUpperInvariant(), nombre = r.nombre.Trim(), descripcion = Clean(r.descripcion), icono = Clean(r.icono), r.orden, r.activo });
        await _db.ExecuteTransaccion(tx, "INSERT INTO seg_modulos_configuracion(cod_modulo,habilitado,fecha_inicio,fecha_fin) VALUES(@cod,@habilitado,@inicio,@fin) ON DUPLICATE KEY UPDATE habilitado=VALUES(habilitado),fecha_inicio=VALUES(fecha_inicio),fecha_fin=VALUES(fecha_fin),updatedday=NOW();", new { cod, habilitado = r.habilitado, inicio = r.fecha_inicio, fin = r.fecha_fin });
        tx.Commit(); return true;
    }
    public async Task<bool> Estado(int cod, bool activo, string conexion)
    {
        var n = await _db.Execute("UPDATE seg_modulos SET activo=@activo,updatedday=NOW() WHERE cod=@cod;", new { cod, activo }, conexion);
        return n > 0 || await Obtener(cod, conexion) is not null;
    }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
