using UniCore.Api.Database;
using UniCore.Api.Modules.Seguridad.Dto;
using UniCore.Api.Modules.Seguridad.Requests;

namespace UniCore.Api.Modules.Seguridad.Managers;

public sealed class RolesManager
{

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
        return DatabaseConnection.GetMany<RolSeguridadDto>(conexion, query, new { busqueda = filtro });
    }

    public async Task<RolDetalleDto?> ObtenerRol(int cod, string conexion)
    {
        const string rolSql = "SELECT cod, codigo, nombre, descripcion, activo, es_superadmin FROM seg_roles WHERE cod = @cod LIMIT 1;";
        var rol = await DatabaseConnection.GetOne<RolSeguridadDto>(conexion, rolSql, new { cod });
        if (rol is null) return null;

        const string permisosSql = @"
SELECT p.cod, p.cod_modulo, p.codigo, p.nombre, p.accion, p.activo, p.descripcion, p.createdday
FROM seg_rol_permisos rp
INNER JOIN seg_permisos p ON p.cod = rp.cod_permiso
WHERE rp.cod_rol = @cod_rol
ORDER BY p.codigo;";
        var permisos = await DatabaseConnection.GetMany<PermisoSeguridadDto>(conexion, permisosSql, new { cod_rol = cod });
        return new RolDetalleDto { rol = rol, permisos = permisos };
    }

    public async Task<bool> CodigoExiste(string codigo, int? excluirCod, string conexion)
    {
        const string sql = "SELECT COUNT(*) FROM seg_roles WHERE codigo = @codigo AND (@excluir IS NULL OR cod <> @excluir);";
        return await DatabaseConnection.ExecuteScalar<long>(conexion, sql, new { codigo, excluir = excluirCod }) > 0;
    }

    public Task<bool> EsRolSuperadmin(int cod, string conexion)
        => DatabaseConnection.ExecuteScalar<bool>(conexion, "SELECT es_superadmin FROM seg_roles WHERE cod=@cod LIMIT 1;", new { cod });

    public async Task<List<int>> PermisosInvalidos(IEnumerable<int> codigos, string conexion)
    {
        var unicos = codigos.Distinct().ToArray();
        if (unicos.Length == 0) return [];
        const string sql = "SELECT cod FROM seg_permisos WHERE activo = 1 AND cod IN @codigos;";
        var validos = await DatabaseConnection.GetMany<int>(conexion, sql, new { codigos = unicos });
        return unicos.Except(validos).ToList();
    }

    public async Task<int> Crear(RolRequest request, string conexion)
    {
        await using var tx = DatabaseConnection.BeginTransaction(conexion);
        var cod = await DatabaseConnection.InsertTransaccion(tx, "seg_roles", new
        {
            codigo = request.codigo.Trim().ToUpperInvariant(),
            nombre = request.nombre.Trim(),
            descripcion = Normalizar(request.descripcion),
            request.activo,
        });
        await ReemplazarPermisos(tx, checked((int)cod), request.cod_permisos);
        tx.Commit();
        return checked((int)cod);
    }

    public async Task<bool> Actualizar(int cod, RolRequest request, string conexion)
    {
        await using var tx = DatabaseConnection.BeginTransaction(conexion);
        var existe = await DatabaseConnection.ExecuteScalarTransaccion<long>(tx, "SELECT COUNT(*) FROM seg_roles WHERE cod = @cod;", new { cod });
        if (existe == 0) return false;

        await DatabaseConnection.UpdateTransaccion(tx, "seg_roles", new
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
        await DatabaseConnection.Update(conexion, "seg_roles", new { cod, activo });
        return await DatabaseConnection.ExecuteScalar<long>(conexion, "SELECT COUNT(*) FROM seg_roles WHERE cod = @cod;", new { cod }) > 0;
    }

    private async Task ReemplazarPermisos(DatabaseTransaction tx, int codRol, IEnumerable<int> codPermisos)
    {
        await DatabaseConnection.DeleteTransaccion(tx, "seg_rol_permisos", new { cod_rol = codRol });
        foreach (var codPermiso in codPermisos.Distinct())
            await DatabaseConnection.InsertTransaccion(tx, "seg_rol_permisos", new { cod_rol = codRol, cod_permiso = codPermiso });
    }

    private static string? Normalizar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
