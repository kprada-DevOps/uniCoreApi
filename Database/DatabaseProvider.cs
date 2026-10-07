using Microsoft.Extensions.DependencyInjection;

namespace UniCore.Api.Database;

/// <summary>
/// Envoltura inyectable de <see cref="DatabaseConnection"/>.
/// Permite consumir la capa de datos desde cualquier servicio resuelto por DI,
/// sin perder el carácter estático de la implementación original.
///
/// Igual que en el proyecto de referencia, la conexión se pasa SIEMPRE de forma
/// explicita: el controller la recibe del segmento de ruta y la propaga.
/// </summary>
public sealed class DatabaseProvider
{
    private string Resolve(string? conexion)
    {
        if (string.IsNullOrWhiteSpace(conexion))
            throw new InvalidOperationException(
                "El nombre de la conexion es obligatorio. El controller debe recibirla de la ruta " +
                "y propagarla al manager, igual que hace el proyecto de referencia.");

        return conexion;
    }

    public Task<List<T>> GetMany<T>(string query, object? filter = null, string? conexion = null)
        => DatabaseConnection.GetMany<T>(Resolve(conexion), query, filter);

    public Task<T?> GetOne<T>(string query, object? filter = null, string? conexion = null)
        => DatabaseConnection.GetOne<T>(Resolve(conexion), query, filter);

    public Task<long> Insert(string table, object filter, string? conexion = null)
        => DatabaseConnection.Insert(Resolve(conexion), table, filter);

    public Task<long> InsertSinDuplicados(string table, object filter, string? conexion = null)
        => DatabaseConnection.InsertSinDuplicados(Resolve(conexion), table, filter);

    public Task<List<long>> InsertMany<T>(string table, IEnumerable<T> filters, string? conexion = null)
        => DatabaseConnection.InsertMany<T>(Resolve(conexion), table, filters);

    public Task<bool> Update(string table, object filter, object? filterWhere = null, bool saveNulls = true, string? conexion = null)
        => DatabaseConnection.Update(Resolve(conexion), table, filter, filterWhere, saveNulls);

    public Task<List<int>> UpdateMany(string table, List<object> filters, List<object>? filterWhere = null, bool saveNulls = true, string? conexion = null)
        => DatabaseConnection.UpdateMany(Resolve(conexion), table, filters, filterWhere, saveNulls);

    public Task<bool> Delete(string table, object? filter, string? customWhere = null, string? conexion = null)
        => DatabaseConnection.Delete(Resolve(conexion), table, filter, customWhere);

    public Task<int> DeleteMany(string table, List<object> filters, string? customWhere = null, string? conexion = null)
        => DatabaseConnection.DeleteMany(Resolve(conexion), table, filters, customWhere);

    public Task<long> Upsert(string table, object filter, string? conexion = null)
        => DatabaseConnection.Upsert(Resolve(conexion), table, filter);

    public Task<int> Execute(string query, object? filter = null, string? conexion = null)
        => DatabaseConnection.Execute(Resolve(conexion), query, filter);

    public Task<T?> ExecuteScalar<T>(string query, object? filter = null, string? conexion = null)
        => DatabaseConnection.ExecuteScalar<T>(Resolve(conexion), query, filter);

    public DatabaseTransaction BeginTransaction(string? conexion = null)
        => DatabaseConnection.BeginTransaction(Resolve(conexion));

    // ---------- Variantes transaccionales ----------

    public Task<List<T>> GetManyTransaccion<T>(DatabaseTransaction tx, string query, object? filter = null)
        => DatabaseConnection.GetManyTransaccion<T>(tx, query, filter);

    public Task<T?> GetOneTransaccion<T>(DatabaseTransaction tx, string query, object? filter = null)
        => DatabaseConnection.GetOneTransaccion<T>(tx, query, filter);

    public Task<long> InsertTransaccion(DatabaseTransaction tx, string table, object filter)
        => DatabaseConnection.InsertTransaccion(tx, table, filter);

    public Task<List<long>> InsertManyTransaccion<T>(DatabaseTransaction tx, string table, IEnumerable<T> filters)
        => DatabaseConnection.InsertManyTransaccion<T>(tx, table, filters);

    public Task<bool> UpdateTransaccion(DatabaseTransaction tx, string table, object filter, object? filterWhere = null, bool saveNulls = true)
        => DatabaseConnection.UpdateTransaccion(tx, table, filter, filterWhere, saveNulls);

    public Task<List<int>> UpdateManyTransaccion(DatabaseTransaction tx, string table, List<object> filters, List<object>? filterWhere = null, bool saveNulls = true)
        => DatabaseConnection.UpdateManyTransaccion(tx, table, filters, filterWhere, saveNulls);

    public Task<bool> DeleteTransaccion(DatabaseTransaction tx, string table, object filter, string? customWhere = null)
        => DatabaseConnection.DeleteTransaccion(tx, table, filter, customWhere);

    public Task<int> DeleteManyTransaccion(DatabaseTransaction tx, string table, List<object> filters, string? customWhere = null)
        => DatabaseConnection.DeleteManyTransaccion(tx, table, filters, customWhere);

    public Task<int> ExecuteTransaccion(DatabaseTransaction tx, string query, object? filter = null)
        => DatabaseConnection.ExecuteTransaccion(tx, query, filter);

    public Task<T?> ExecuteScalarTransaccion<T>(DatabaseTransaction tx, string query, object? filter = null)
        => DatabaseConnection.ExecuteScalarTransaccion<T>(tx, query, filter);
}
