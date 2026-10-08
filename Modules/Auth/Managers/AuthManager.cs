using UniCore.Api.Database;
using UniCore.Api.Modules.Auth.Dto;

namespace UniCore.Api.Modules.Auth.Managers;

/// <summary>
/// Acceso a los datos de seguridad del usuario: autenticación, roles y permisos.
/// Traduce el esquema de las tablas seg_* en objetos de dominio.
/// </summary>
public sealed class AuthManager
{
    private const string ObtenerUsuarioSql = @"
SELECT
    cod           AS cod,
    cod_persona   AS cod_persona,
    username      AS username,
    password_hash AS password_hash,
    activo        AS activo,
    ultimo_acceso AS ultimo_acceso
FROM seg_usuarios
WHERE username = @username
LIMIT 1;";

    private const string ObtenerUsuarioPorCodSql = @"
SELECT
    cod           AS cod,
    cod_persona   AS cod_persona,
    username      AS username,
    password_hash AS password_hash,
    activo        AS activo,
    ultimo_acceso AS ultimo_acceso
FROM seg_usuarios
WHERE cod = @cod
LIMIT 1;";

    private const string ObtenerRolesSql = @"
SELECT DISTINCT
    r.codigo AS codigo
FROM seg_usuario_roles ur
INNER JOIN seg_roles r ON r.cod = ur.cod_rol
WHERE ur.cod_usuario = @cod_usuario
  AND r.activo = 1
ORDER BY r.codigo;";

    private const string ObtenerPermisosSql = @"
SELECT DISTINCT
    p.codigo AS codigo
FROM seg_usuario_roles ur
INNER JOIN seg_roles r ON r.cod = ur.cod_rol
INNER JOIN seg_rol_permisos rp ON rp.cod_rol = ur.cod_rol
INNER JOIN seg_permisos p ON p.cod = rp.cod_permiso
INNER JOIN seg_modulos m ON m.cod = p.cod_modulo AND m.activo = 1
LEFT JOIN seg_modulos_configuracion mc ON mc.cod_modulo = m.cod
WHERE ur.cod_usuario = @cod_usuario
  AND r.activo = 1
  AND p.activo = 1
  AND (mc.cod IS NULL OR (mc.habilitado = 1
    AND (mc.fecha_inicio IS NULL OR mc.fecha_inicio <= NOW())
    AND (mc.fecha_fin IS NULL OR mc.fecha_fin >= NOW())))
ORDER BY p.codigo;";

    private const string EsSuperadminSql = @"
SELECT EXISTS(
    SELECT 1
    FROM seg_usuario_roles ur
    INNER JOIN seg_roles r ON r.cod = ur.cod_rol
    WHERE ur.cod_usuario = @cod_usuario AND r.activo = 1 AND r.es_superadmin = 1
);";

    private const string ActualizarUltimoAccesoSql = @"
UPDATE seg_usuarios
SET ultimo_acceso = @ultimo_acceso
WHERE cod = @cod;";

    private readonly DatabaseProvider _database;

    public AuthManager(DatabaseProvider database)
    {
        _database = database;
    }

    /// <summary>
    /// Busca un usuario por nombre, sin filtrar por activo: la comprobación
    /// del estado se hace en la capa de autenticación para poder distinguir
    /// "desactivado" de "no existe" en el log interno.
    /// </summary>
    public async Task<UsuarioDto?> ObtenerUsuarioPorUsername(string username, string conexion)
    {
        return await _database.GetOne<UsuarioDto>(ObtenerUsuarioSql, new { username }, conexion);
    }

    /// <summary>
    /// Busca un usuario por su clave primaria. Usada por el refresh y por los
    /// endpoints que leen la identidad del token.
    /// </summary>
    public async Task<UsuarioDto?> ObtenerUsuarioPorCod(long cod, string conexion)
    {
        return await _database.GetOne<UsuarioDto>(ObtenerUsuarioPorCodSql, new { cod }, conexion);
    }

    public async Task<List<string>> ObtenerRolesUsuario(long codUsuario, string conexion)
    {
        return await _database.GetMany<string>(ObtenerRolesSql, new { cod_usuario = codUsuario }, conexion);
    }

    public async Task<List<string>> ObtenerPermisosUsuario(long codUsuario, string conexion)
    {
        return await _database.GetMany<string>(ObtenerPermisosSql, new { cod_usuario = codUsuario }, conexion);
    }

    /// <summary>
    /// Rellena roles y permisos sobre el usuario dado.
    /// </summary>
    public async Task<UsuarioDto> CompletarPermisos(UsuarioDto usuario, string conexion)
    {
        usuario.roles = await ObtenerRolesUsuario(usuario.cod, conexion);
        usuario.permisos = await ObtenerPermisosUsuario(usuario.cod, conexion);
        usuario.es_superadmin = await EsSuperadmin(usuario.cod, conexion);
        return usuario;
    }

    public Task<bool> EsSuperadmin(long codUsuario, string conexion)
        => _database.ExecuteScalar<bool>(EsSuperadminSql, new { cod_usuario = codUsuario }, conexion);

    /// <summary>
    /// Registra el último acceso con la fecha ajustada a la zona horaria de la conexión.
    /// Un fallo aquí no debe impedir el login: la auditoría es secundaria.
    /// </summary>
    public async Task ActualizarUltimoAcceso(long codUsuario, string conexion)
    {
        try
        {
            var fecha = DbHelpers.GetFechaActual(conexion);
            await _database.Execute(ActualizarUltimoAccesoSql,
                new { ultimo_acceso = fecha, cod = codUsuario }, conexion);
        }
        catch (Exception)
        {
            // Se omite: el login ya es válido en este punto.
        }
    }
}
