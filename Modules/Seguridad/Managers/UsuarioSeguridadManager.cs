using UniCore.Api.Database;
using UniCore.Api.Modules.Seguridad.Dto;
using UniCore.Api.Modules.Seguridad.Requests;

namespace UniCore.Api.Modules.Seguridad.Managers;

/// <summary>Consultas y escrituras administrativas de usuarios de Seguridad.</summary>
public sealed class UsuarioSeguridadManager
{
    private const string ColumnasUsuario = @"
    u.cod AS cod,
    u.cod_persona AS cod_persona,
    u.username AS username,
    u.activo AS activo,
    u.ultimo_acceso AS ultimo_acceso,
    u.createdday AS createdday,
    u.updatedday AS updatedday";

    private readonly DatabaseProvider _database;

    public UsuarioSeguridadManager(DatabaseProvider database)
    {
        _database = database;
    }

    public async Task<ResultadoPaginadoDto<UsuarioSeguridadDto>> ObtenerUsuarios(
        string conexion,
        bool incluirInactivos,
        string? busqueda,
        int pagina,
        int tamanoPagina)
    {
        pagina = Math.Max(1, pagina);
        tamanoPagina = Math.Clamp(tamanoPagina, 1, 100);
        var filtroEstado = incluirInactivos ? string.Empty : "AND u.activo = 1";
        var filtroBusqueda = @"WHERE (@busqueda IS NULL
    OR u.username LIKE @busqueda
    OR p.numero_documento LIKE @busqueda
    OR CONCAT_WS(' ', p.primer_nombre, p.segundo_nombre, p.primer_apellido, p.segundo_apellido) LIKE @busqueda)";
        var condicion = $"{filtroBusqueda} {filtroEstado}";
        var parametros = new
        {
            busqueda = string.IsNullOrWhiteSpace(busqueda) ? null : $"%{busqueda.Trim()}%",
            offset = ((long)pagina - 1) * tamanoPagina,
            tamano = tamanoPagina,
        };
        const string from = @"
FROM seg_usuarios u
LEFT JOIN per_personas p ON p.cod = u.cod_persona";
        var total = await _database.ExecuteScalar<long>(
            $"SELECT COUNT(*) {from} {condicion};", parametros, conexion);
        var query = $@"
SELECT{ColumnasUsuario}
{from}
{condicion}
ORDER BY u.username, u.cod
LIMIT @tamano OFFSET @offset;";

        var items = await _database.GetMany<UsuarioSeguridadDto>(query, parametros, conexion);
        return new ResultadoPaginadoDto<UsuarioSeguridadDto>
        {
            items = items,
            pagina = pagina,
            tamanoPagina = tamanoPagina,
            total = total,
        };
    }

    public async Task<UsuarioDetalleDto?> ObtenerUsuario(long cod, string conexion)
    {
        const string query = $@"
SELECT{ColumnasUsuario}
FROM seg_usuarios u
WHERE u.cod = @cod
LIMIT 1;";

        var usuario = await _database.GetOne<UsuarioSeguridadDto>(query, new { cod }, conexion);
        if (usuario is null) return null;

        const string rolesQuery = @"
SELECT r.cod AS cod, r.codigo AS codigo, r.nombre AS nombre,
       r.descripcion AS descripcion, r.activo AS activo
FROM seg_usuario_roles ur
INNER JOIN seg_roles r ON r.cod = ur.cod_rol
WHERE ur.cod_usuario = @cod_usuario
ORDER BY r.codigo;";

        var roles = await _database.GetMany<RolSeguridadDto>(rolesQuery, new { cod_usuario = cod }, conexion);
        return new UsuarioDetalleDto { usuario = usuario, roles = roles };
    }

    public Task<List<RolSeguridadDto>> ObtenerRoles(string conexion)
    {
        const string query = @"
SELECT cod, codigo, nombre, descripcion, activo
FROM seg_roles
WHERE activo = 1
ORDER BY activo DESC, codigo;";
        return _database.GetMany<RolSeguridadDto>(query, conexion: conexion);
    }

    public async Task<bool> UsernameExiste(string username, long? excluirCod, string conexion)
    {
        const string query = @"
SELECT COUNT(*)
FROM seg_usuarios
WHERE username = @username AND (@excluir_cod IS NULL OR cod <> @excluir_cod);";
        return await _database.ExecuteScalar<long>(query,
            new { username, excluir_cod = excluirCod }, conexion) > 0;
    }

    public async Task<bool> PersonaExiste(long? codPersona, string conexion)
    {
        if (!codPersona.HasValue) return true;
        const string query = "SELECT COUNT(*) FROM per_personas WHERE cod = @cod;";
        return await _database.ExecuteScalar<long>(query, new { cod = codPersona.Value }, conexion) > 0;
    }

    public async Task<List<int>> RolesInvalidos(IEnumerable<int> codRoles, string conexion, long? codUsuario = null)
    {
        var codigos = codRoles.Distinct().ToArray();
        if (codigos.Length == 0) return [];

        const string query = @"
SELECT r.cod
FROM seg_roles r
LEFT JOIN seg_usuario_roles ur ON ur.cod_rol = r.cod AND ur.cod_usuario = @cod_usuario
WHERE r.cod IN @codigos AND (r.activo = 1 OR ur.cod_usuario IS NOT NULL);";
        var validos = await _database.GetMany<int>(query, new { codigos, cod_usuario = codUsuario }, conexion);
        return codigos.Except(validos).ToList();
    }

    public async Task<long> CrearUsuario(UsuarioCrearRequest request, string conexion)
    {
        const string insert = @"
INSERT INTO seg_usuarios (cod_persona, username, password_hash, activo)
VALUES (@cod_persona, @username, @password_hash, @activo);";

        var roles = request.cod_roles.Distinct().ToArray();
        await using var transaccion = _database.BeginTransaction(conexion);

        await _database.ExecuteTransaccion(transaccion, insert, new
        {
            cod_persona = request.cod_persona,
            username = request.username.Trim(),
            password_hash = BCrypt.Net.BCrypt.HashPassword(request.password),
            request.activo,
        });
        var cod = await _database.ExecuteScalarTransaccion<long>(transaccion, "SELECT LAST_INSERT_ID();");
        await ReemplazarRoles(transaccion, cod, roles);
        transaccion.Commit();
        return cod;
    }

    public async Task<bool> ActualizarUsuario(long cod, UsuarioActualizarRequest request, string conexion)
    {
        await using var transaccion = _database.BeginTransaction(conexion);
        var existe = await _database.ExecuteScalarTransaccion<long>(transaccion,
            "SELECT COUNT(*) FROM seg_usuarios WHERE cod = @cod;", new { cod });
        if (existe == 0) return false;

        const string update = @"
UPDATE seg_usuarios
SET cod_persona = @cod_persona,
    username = @username,
    activo = @activo,
    updatedday = @updatedday
WHERE cod = @cod;";

        await _database.ExecuteTransaccion(transaccion, update, new
        {
            cod,
            cod_persona = request.cod_persona,
            username = request.username.Trim(),
            request.activo,
            updatedday = DbHelpers.GetFechaActualDatetime(conexion),
        });

        await ReemplazarRoles(transaccion, cod, request.cod_roles.Distinct().ToArray());
        transaccion.Commit();
        return true;
    }

    public async Task<bool> EstablecerActivo(long cod, bool activo, string conexion)
    {
        const string query = @"
UPDATE seg_usuarios
SET activo = @activo, updatedday = @updatedday
WHERE cod = @cod;";
        await _database.Execute(query,
            new { cod, activo, updatedday = DbHelpers.GetFechaActualDatetime(conexion) }, conexion);

        // MySQL puede informar cero filas modificadas si el estado ya tenía ese valor.
        var existe = await _database.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM seg_usuarios WHERE cod = @cod;", new { cod }, conexion);
        return existe > 0;
    }

    public async Task<bool> CambiarContrasena(long cod, string password, string conexion)
    {
        const string query = @"
UPDATE seg_usuarios
SET password_hash = @password_hash, updatedday = @updatedday
WHERE cod = @cod;";
        await _database.Execute(query, new
        {
            cod,
            password_hash = BCrypt.Net.BCrypt.HashPassword(password),
            updatedday = DbHelpers.GetFechaActualDatetime(conexion),
        }, conexion);
        return await _database.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM seg_usuarios WHERE cod = @cod;", new { cod }, conexion) > 0;
    }

    private async Task ReemplazarRoles(DatabaseTransaction transaccion, long codUsuario, IEnumerable<int> codRoles)
    {
        await _database.ExecuteTransaccion(transaccion,
            "DELETE FROM seg_usuario_roles WHERE cod_usuario = @cod_usuario;",
            new { cod_usuario = codUsuario });

        const string insert = @"
INSERT INTO seg_usuario_roles (cod_usuario, cod_rol)
VALUES (@cod_usuario, @cod_rol);";
        foreach (var codRol in codRoles)
        {
            await _database.ExecuteTransaccion(transaccion, insert,
                new { cod_usuario = codUsuario, cod_rol = codRol });
        }
    }
}
