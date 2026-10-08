using UniCore.Api.Database;
using UniCore.Api.Modules.Seguridad.Dto;
using UniCore.Api.Modules.Seguridad.Requests;

namespace UniCore.Api.Modules.Seguridad.Managers;

public sealed class RolesManager
{
    private readonly DatabaseProvider _database;

    public RolesManager(DatabaseProvider database) => _database = database;

    public Task<List<RolSeguridadDto>> ObtenerRoles(string conexion, bool incluirInactivos, string? busqueda)
    {
        var estado = incluirInactivos ? string.Empty : "AND activo = 1";
        var query = $@"
SELECT cod, codigo, nombre, descripcion, activo, es_superadmin
FROM seg_roles
WHERE es_superadmin=0 AND (@busqueda IS NULL OR codigo LIKE @busqueda OR nombre LIKE @busqueda)
{estado}
ORDER BY codigo;";
        var filtro = string.IsNullOrWhiteSpace(busqueda) ? null : $"%{busqueda.Trim()}%";
        return _database.GetMany<RolSeguridadDto>(query, new { busqueda = filtro }, conexion);
    }

    public async Task<RolDetalleDto?> ObtenerRol(int cod, string conexion)
    {
        const string rolSql = "SELECT cod, codigo, nombre, descripcion, activo, es_superadmin FROM seg_roles WHERE cod = @cod LIMIT 1;";
        var rol = await _database.GetOne<RolSeguridadDto>(rolSql, new { cod }, conexion);
        if (rol is null) return null;

        const string permisosSql = @"
SELECT p.cod, p.cod_modulo, p.codigo, p.nombre, p.accion, p.activo, p.descripcion, p.createdday
FROM seg_rol_permisos rp
INNER JOIN seg_permisos p ON p.cod = rp.cod_permiso
WHERE rp.cod_rol = @cod_rol
ORDER BY p.codigo;";
        var permisos = await _database.GetMany<PermisoSeguridadDto>(permisosSql, new { cod_rol = cod }, conexion);
        return new RolDetalleDto { rol = rol, permisos = permisos };
    }

    public async Task<bool> CodigoExiste(string codigo, int? excluirCod, string conexion)
    {
        const string sql = "SELECT COUNT(*) FROM seg_roles WHERE codigo = @codigo AND (@excluir IS NULL OR cod <> @excluir);";
        return await _database.ExecuteScalar<long>(sql, new { codigo, excluir = excluirCod }, conexion) > 0;
    }

    public Task<bool> EsRolSuperadmin(int cod, string conexion)
        => _database.ExecuteScalar<bool>("SELECT es_superadmin FROM seg_roles WHERE cod=@cod LIMIT 1;", new { cod }, conexion);

    public async Task<List<int>> PermisosInvalidos(IEnumerable<int> codigos, string conexion)
    {
        var unicos = codigos.Distinct().ToArray();
        if (unicos.Length == 0) return [];
        const string sql = "SELECT cod FROM seg_permisos WHERE activo = 1 AND cod IN @codigos;";
        var validos = await _database.GetMany<int>(sql, new { codigos = unicos }, conexion);
        return unicos.Except(validos).ToList();
    }

    public async Task<int> Crear(RolRequest request, string conexion)
    {
        await using var tx = _database.BeginTransaction(conexion);
        const string sql = "INSERT INTO seg_roles (codigo, nombre, descripcion, activo) VALUES (@codigo, @nombre, @descripcion, @activo);";
        await _database.ExecuteTransaccion(tx, sql, new
        {
            codigo = request.codigo.Trim().ToUpperInvariant(),
            nombre = request.nombre.Trim(),
            descripcion = Normalizar(request.descripcion),
            request.activo,
        });
        var cod = await _database.ExecuteScalarTransaccion<int>(tx, "SELECT LAST_INSERT_ID();");
        await ReemplazarPermisos(tx, cod, request.cod_permisos);
        tx.Commit();
        return cod;
    }

    public async Task<bool> Actualizar(int cod, RolRequest request, string conexion)
    {
        await using var tx = _database.BeginTransaction(conexion);
        var existe = await _database.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM seg_roles WHERE cod = @cod;", new { cod }, conexion);
        if (existe == 0) return false;

        const string sql = @"
UPDATE seg_roles SET codigo = @codigo, nombre = @nombre, descripcion = @descripcion, activo = @activo
WHERE cod = @cod;";
        await _database.ExecuteTransaccion(tx, sql, new
        {
            cod,
            codigo = request.codigo.Trim().ToUpperInvariant(),
            nombre = request.nombre.Trim(),
            descripcion = Normalizar(request.descripcion),
            request.activo,
        });
        await ReemplazarPermisos(tx, cod, request.cod_permisos);
        tx.Commit();
        return true;
    }

    public async Task<bool> EstablecerActivo(int cod, bool activo, string conexion)
    {
        await _database.Execute("UPDATE seg_roles SET activo = @activo WHERE cod = @cod;", new { cod, activo }, conexion);
        return await _database.ExecuteScalar<long>("SELECT COUNT(*) FROM seg_roles WHERE cod = @cod;", new { cod }, conexion) > 0;
    }

    private async Task ReemplazarPermisos(DatabaseTransaction tx, int codRol, IEnumerable<int> codPermisos)
    {
        await _database.ExecuteTransaccion(tx, "DELETE FROM seg_rol_permisos WHERE cod_rol = @cod_rol;", new { cod_rol = codRol });
        const string sql = "INSERT INTO seg_rol_permisos (cod_rol, cod_permiso) VALUES (@cod_rol, @cod_permiso);";
        foreach (var codPermiso in codPermisos.Distinct())
            await _database.ExecuteTransaccion(tx, sql, new { cod_rol = codRol, cod_permiso = codPermiso });
    }

    private static string? Normalizar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
