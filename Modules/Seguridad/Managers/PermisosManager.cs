using UniCore.Api.Database;
using UniCore.Api.Modules.Seguridad.Dto;
using UniCore.Api.Modules.Seguridad.Requests;

namespace UniCore.Api.Modules.Seguridad.Managers;

public sealed class PermisosManager
{
    private const string Select = "SELECT cod, cod_modulo, codigo, nombre, accion, activo, descripcion, createdday FROM seg_permisos";

    public Task<List<PermisoSeguridadDto>> Listar(string conexion, int? modulo, bool incluirInactivos, string? busqueda)
        => DatabaseConnection.GetMany<PermisoSeguridadDto>(conexion, $"{Select} WHERE (@modulo IS NULL OR cod_modulo=@modulo) AND (@inactivos=1 OR activo=1) AND (@busqueda IS NULL OR codigo LIKE @busqueda OR nombre LIKE @busqueda) ORDER BY cod_modulo,codigo;", new { modulo, inactivos = incluirInactivos, busqueda = string.IsNullOrWhiteSpace(busqueda) ? null : $"%{busqueda.Trim()}%" });

    public Task<PermisoSeguridadDto?> Obtener(int cod, string conexion)
        => DatabaseConnection.GetOne<PermisoSeguridadDto>(conexion, $"{Select} WHERE cod=@cod LIMIT 1;", new { cod });

    public Task<long?> ExisteCodigo(string codigo, int? omitir, string conexion)
        => DatabaseConnection.ExecuteScalar<long?>(conexion, "SELECT cod FROM seg_permisos WHERE codigo=@codigo AND (@omitir IS NULL OR cod<>@omitir) LIMIT 1;", new { codigo, omitir });

    public async Task<bool> ModuloExiste(int cod, string conexion)
        => await DatabaseConnection.ExecuteScalar<long>(conexion, "SELECT COUNT(*) FROM seg_modulos WHERE cod=@cod;", new { cod }) > 0;

    public async Task<int> Crear(PermisoRequest r, string conexion)
    {
        await using var tx = DatabaseConnection.BeginTransaction(conexion);
        var id = checked((int)await DatabaseConnection.InsertTransaccion(tx, "seg_permisos", CrearParam(r)));
        tx.Commit(); return id;
    }

    public async Task<bool> Actualizar(int cod, PermisoRequest r, string conexion)
        => await DatabaseConnection.Update(conexion, "seg_permisos", Param(r, cod));

    public async Task<bool> Estado(int cod, bool activo, string conexion)
    {
        var n = await DatabaseConnection.Update(conexion, "seg_permisos", new { cod, activo });
        return n || await Obtener(cod, conexion) is not null;
    }

    private static object CrearParam(PermisoRequest r) => new { r.cod_modulo, codigo = r.codigo.Trim().ToUpperInvariant(), nombre = r.nombre.Trim(), accion = r.accion.ToUpperInvariant(), r.activo, descripcion = string.IsNullOrWhiteSpace(r.descripcion) ? null : r.descripcion.Trim() };
    private static object Param(PermisoRequest r, int? cod = null) => new { cod, r.cod_modulo, codigo = r.codigo.Trim().ToUpperInvariant(), nombre = r.nombre.Trim(), accion = r.accion.ToUpperInvariant(), r.activo, descripcion = string.IsNullOrWhiteSpace(r.descripcion) ? null : r.descripcion.Trim() };
}
