using Dapper;

namespace UniCore.Api.Database;

/// <summary>
/// Acceso genérico a datos sobre MySQL mediante Dapper.
/// El nombre de conexión se resuelve en cada llamada desde la configuración.
/// </summary>
public static class DatabaseConnection
{
    // ---------- Lectura ----------

    public static async Task<List<T>> GetMany<T>(string conexion, string query, object? filter = null)
    {
        await using var connection = MysqlConnectionFactory.CreateConnection(conexion);
        var response = await connection.QueryAsync<T>(query, filter);
        return response.ToList() ?? throw new Exception("Error al obtener la lista de datos");
    }

    public static async Task<List<T>> GetManyTransaccion<T>(DatabaseTransaction tx, string query, object? filter = null)
    {
        var response = await tx.Connection.QueryAsync<T>(query, filter, tx.Transaction);
        return response.ToList() ?? throw new Exception("Error al obtener la lista de datos");
    }

    public static async Task<T?> GetOne<T>(string conexion, string query, object? filter = null)
    {
        await using var connection = MysqlConnectionFactory.CreateConnection(conexion);
        return await connection.QueryFirstOrDefaultAsync<T>(query, filter);
    }

    public static async Task<T?> GetOneTransaccion<T>(DatabaseTransaction tx, string query, object? filter = null)
    {
        return await tx.Connection.QueryFirstOrDefaultAsync<T>(query, filter, tx.Transaction);
    }

    // ---------- Transacciones ----------

    public static DatabaseTransaction BeginTransaction(string conexion) => new(conexion);

    // ---------- Insert ----------

    public static async Task<long> Insert(string conexion, string table, object filter)
    {
        await using var connection = MysqlConnectionFactory.CreateConnection(conexion);
        var properties = GetProperties(filter);
        var columns = string.Join(",", properties.Select(p => p.Name));
        var values = string.Join(",", properties.Select(p => $"@{p.Name}"));
        var query = $"INSERT INTO {table} ({columns}) VALUES ({values}); SELECT LAST_INSERT_ID();";
        return await connection.ExecuteScalarAsync<long>(query, filter);
    }

    public static async Task<long> InsertTransaccion(DatabaseTransaction tx, string table, object filter)
    {
        var properties = GetProperties(filter);
        var columns = string.Join(",", properties.Select(p => p.Name));
        var values = string.Join(",", properties.Select(p => $"@{p.Name}"));
        var query = $"INSERT INTO {table} ({columns}) VALUES ({values}); SELECT LAST_INSERT_ID();";
        return await tx.Connection.ExecuteScalarAsync<long>(query, filter, tx.Transaction);
    }

    /// <summary>
    /// Inserta ignorando duplicados. Devuelve el id si insertó, 0 si la fila ya existía.
    /// </summary>
    public static async Task<long> InsertSinDuplicados(string conexion, string table, object filter)
    {
        await using var connection = MysqlConnectionFactory.CreateConnection(conexion);
        var properties = GetProperties(filter);
        var columns = string.Join(",", properties.Select(p => p.Name));
        var values = string.Join(",", properties.Select(p => $"@{p.Name}"));
        var query = $@"
INSERT INTO {table} ({columns}) VALUES ({values})
ON DUPLICATE KEY UPDATE {properties[0].Name} = {properties[0].Name};
SELECT LAST_INSERT_ID();";
        return await connection.ExecuteScalarAsync<long>(query, filter);
    }

    public static async Task<List<long>> InsertMany<T>(string conexion, string table, IEnumerable<T> filters)
    {
        await using var connection = MysqlConnectionFactory.CreateConnection(conexion);
        var materializado = filters as IList<T> ?? filters.ToList();
        var (queries, allFilters) = BuildInsertMany(table, materializado);
        var result = await connection.ExecuteScalarAsync<long>(
            string.Format("{0}; SELECT LAST_INSERT_ID();", string.Join(";", queries)), allFilters);
        return DbHelpers.CreateRange(result - (materializado.Count - 1), materializado.Count).ToList();
    }

    public static async Task<List<long>> InsertManyTransaccion<T>(DatabaseTransaction tx, string table, IEnumerable<T> filters)
    {
        var materializado = filters as IList<T> ?? filters.ToList();
        var (queries, allFilters) = BuildInsertMany(table, materializado);
        var result = await tx.Connection.ExecuteScalarAsync<long>(
            string.Format("{0}; SELECT LAST_INSERT_ID();", string.Join(";", queries)), allFilters, tx.Transaction);
        return DbHelpers.CreateRange(result - (materializado.Count - 1), materializado.Count).ToList();
    }

    // ---------- Update ----------

    public static async Task<bool> Update(string conexion, string table, object filter, object? filterWhere = null, bool saveNulls = true)
    {
        await using var connection = MysqlConnectionFactory.CreateConnection(conexion);
        var query = BuildUpdate(table, filter, filterWhere, saveNulls, index: null);
        var affectedRows = await connection.ExecuteAsync(query, DbHelpers.ConcatObjects(new List<object?> { filter, filterWhere }));
        return affectedRows > 0;
    }

    public static async Task<bool> UpdateTransaccion(DatabaseTransaction tx, string table, object filter, object? filterWhere = null, bool saveNulls = true)
    {
        var query = BuildUpdate(table, filter, filterWhere, saveNulls, index: null);
        var affectedRows = await tx.Connection.ExecuteAsync(query,
            DbHelpers.ConcatObjects(new List<object?> { filter, filterWhere }), tx.Transaction);
        return affectedRows > 0;
    }

    public static async Task<List<int>> UpdateMany(string conexion, string table, List<object> filters, List<object>? filterWhere = null, bool saveNulls = true)
    {
        await using var connection = MysqlConnectionFactory.CreateConnection(conexion);
        var (queries, allFilters) = BuildUpdateMany(table, filters, filterWhere, saveNulls);
        var result = await connection.ExecuteScalarAsync<long>(
            string.Format("{0}; SELECT LAST_INSERT_ID();", string.Join(";", queries)), allFilters);
        return Enumerable.Range(Convert.ToInt32(result), filters.Count).ToList();
    }

    public static async Task<List<int>> UpdateManyTransaccion(DatabaseTransaction tx, string table, List<object> filters, List<object>? filterWhere = null, bool saveNulls = true)
    {
        var (queries, allFilters) = BuildUpdateMany(table, filters, filterWhere, saveNulls);
        var result = await tx.Connection.ExecuteScalarAsync<long>(
            string.Format("{0}; SELECT LAST_INSERT_ID();", string.Join(";", queries)), allFilters, tx.Transaction);
        return Enumerable.Range(Convert.ToInt32(result), filters.Count).ToList();
    }

    // ---------- Delete ----------

    public static async Task<bool> Delete(string conexion, string table, object? filter, string? custom_where = null)
    {
        await using var connection = MysqlConnectionFactory.CreateConnection(conexion);
        var where = custom_where ?? BuildWhereFromFilter(filter, null);
        var affectedRows = await connection.ExecuteAsync($"DELETE FROM {table} WHERE {where}", filter);
        return affectedRows > 0;
    }

    public static async Task<bool> DeleteTransaccion(DatabaseTransaction tx, string table, object filter, string? custom_where = null)
    {
        var where = custom_where ?? BuildWhereFromFilter(filter, null);
        var affectedRows = await tx.Connection.ExecuteAsync($"DELETE FROM {table} WHERE {where}", filter, tx.Transaction);
        return affectedRows > 0;
    }

    public static async Task<int> DeleteMany(string conexion, string table, List<object> filters, string? custom_where = null)
    {
        await using var connection = MysqlConnectionFactory.CreateConnection(conexion);
        var (queries, allFilters) = BuildDeleteMany(table, filters, custom_where);
        return await connection.ExecuteAsync(string.Join(";", queries), allFilters);
    }

    public static async Task<int> DeleteManyTransaccion(DatabaseTransaction tx, string table, List<object> filters, string? custom_where = null)
    {
        var (queries, allFilters) = BuildDeleteMany(table, filters, custom_where);
        return await tx.Connection.ExecuteAsync(string.Join(";", queries), allFilters, tx.Transaction);
    }

    // ---------- Upsert ----------

    public static async Task<long> Upsert(string conexion, string table, object filter)
    {
        await using var connection = MysqlConnectionFactory.CreateConnection(conexion);
        var properties = GetProperties(filter);
        var columns = string.Join(",", properties.Select(p => p.Name));
        var values = string.Join(",", properties.Select(p => $"@{p.Name}"));
        var update = string.Join(",", properties.Select(p => $"{p.Name} = @{p.Name}"));
        var query = $"INSERT INTO {table} ({columns}) VALUES ({values}) ON DUPLICATE KEY UPDATE {update}; SELECT LAST_INSERT_ID();";
        return await connection.ExecuteScalarAsync<long>(query, filter);
    }

    // ---------- Ejecución de SQL arbitrario ----------

    public static async Task<int> Execute(string conexion, string query, object? filter = null)
    {
        await using var connection = MysqlConnectionFactory.CreateConnection(conexion);
        return await connection.ExecuteAsync(query, filter);
    }

    public static async Task<int> ExecuteTransaccion(DatabaseTransaction tx, string query, object? filter = null)
    {
        return await tx.Connection.ExecuteAsync(query, filter, tx.Transaction);
    }

    public static async Task<T?> ExecuteScalar<T>(string conexion, string query, object? filter = null)
    {
        await using var connection = MysqlConnectionFactory.CreateConnection(conexion);
        return await connection.ExecuteScalarAsync<T>(query, filter);
    }

    public static async Task<T?> ExecuteScalarTransaccion<T>(DatabaseTransaction tx, string query, object? filter = null)
    {
        return await tx.Connection.ExecuteScalarAsync<T>(query, filter, tx.Transaction);
    }

    // ---------- Construcción de SQL ----------

    /// <summary>
    /// Columna de una sentencia y su valor ya normalizado.
    /// </summary>
    private readonly record struct ColumnValue(string Name, object? Value);

    /// <summary>
    /// Propiedades del filtro con el valor normalizado: DateTime a texto ISO de MySQL,
    /// double/float a invariante cultural, null conservado como null.
    /// </summary>
    private static List<ColumnValue> GetProperties(object filter)
    {
        return filter.GetType().GetProperties()
            .Where(p => p.CanRead)
            .Select(p => new ColumnValue(p.Name, NormalizeValue(p.GetValue(filter))))
            .ToList();
    }

    private static object? NormalizeValue(object? value)
    {
        return value switch
        {
            DateTime dateTime => dateTime.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
            double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
            float f => f.ToString(System.Globalization.CultureInfo.InvariantCulture),
            _ => value,
        };
    }

    private static (List<string> Queries, Dictionary<string, object?> Filters) BuildInsertMany<T>(
        string table, IEnumerable<T> filters)
    {
        var queries = new List<string>();
        var allFilters = new Dictionary<string, object?>();

        foreach (var (filter, index) in filters.Select((value, i) => (value, i)))
        {
            if (filter == null) continue;
            var properties = GetProperties(filter);
            var columns = string.Join(",", properties.Select(p => p.Name));
            var values = string.Join(",", properties.Select(p => $"@{p.Name}{index}"));
            queries.Add($"INSERT INTO {table} ({columns}) VALUES ({values})");
            foreach (var property in properties)
                allFilters[property.Name + index] = property.Value;
        }

        return (queries, allFilters);
    }

    private static (List<string> Queries, Dictionary<string, object?> Filters) BuildUpdateMany(
        string table, List<object> filters, List<object>? filterWhere, bool saveNulls)
    {
        var queries = new List<string>();
        var allFilters = new Dictionary<string, object?>();

        var wheres = filterWhere?.Select((item, index) =>
            BuildWhereFromFilter(item, index)).ToList();

        foreach (var (filter, index) in filters.Select((value, i) => (value, i)))
        {
            var properties = GetProperties(filter);
            var values = string.Join(",", properties
                .Where(p => p.Value != null || saveNulls)
                .Select(p => $"{p.Name} = @{p.Name}{index}"));
            var where = filterWhere != null
                ? wheres![index]
                : $"cod = @cod{index}";
            queries.Add($"UPDATE {table} SET {values} WHERE {where}");

            foreach (var property in properties)
                allFilters[property.Name + index] = property.Value;
        }

        return (queries, allFilters);
    }

    private static string BuildUpdate(string table, object filter, object? filterWhere, bool saveNulls, int? index)
    {
        var suffix = index.HasValue ? index.Value.ToString() : string.Empty;

        var properties = GetProperties(filter);
        var values = string.Join(",", properties
            .Where(p => p.Name != "cod" && (p.Value != null || saveNulls))
            .Select(p => $"{p.Name} = @{p.Name}{suffix}"));

        // Sin filtros adicionales, el criterio por defecto es la clave primaria 'cod'.
        var where = filterWhere != null && filterWhere.GetType().GetProperties().Any()
            ? BuildWhereFromFilter(filterWhere, index)
            : $"cod = @cod{suffix}";

        return $"UPDATE {table} SET {values} WHERE {where}";
    }

    private static (List<string> Queries, Dictionary<string, object?> Filters) BuildDeleteMany(
        string table, List<object> filters, string? custom_where)
    {
        var queries = new List<string>();
        var allFilters = new Dictionary<string, object?>();

        foreach (var (filter, index) in filters.Select((value, i) => (value, i)))
        {
            var where = custom_where ?? BuildWhereFromFilter(filter, index);
            queries.Add($"DELETE FROM {table} WHERE {where}");

            foreach (var property in GetProperties(filter))
            {
                if (property.Value != null)
                    allFilters[property.Name + index] = property.Value;
            }
        }

        return (queries, allFilters);
    }

    /// <summary>
    /// Construye la cláusula WHERE a partir de las propiedades no nulas del filtro.
    /// </summary>
    private static string BuildWhereFromFilter(object? filter, int? index)
    {
        if (filter == null) return string.Empty;
        var suffix = index.HasValue ? index.Value.ToString() : string.Empty;
        return string.Join(" AND ", GetProperties(filter)
            .Where(p => p.Value != null)
            .Select(p => $"{p.Name} = @{p.Name}{suffix}"));
    }
}
