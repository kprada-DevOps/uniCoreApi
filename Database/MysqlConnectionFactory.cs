using MySql.Data.MySqlClient;

namespace UniCore.Api.Database;

/// <summary>
/// Fábrica única de conexiones MySQL. Resuelve el nombre de conexión contra la configuración.
/// </summary>
public static class MysqlConnectionFactory
{
    public static MySqlConnection CreateConnection(string conexion)
    {
        var connectionString = AppSettingsCache.GetConnectionString(conexion);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException($"No existe una conexión configurada con el nombre '{conexion}'.");

        return new MySqlConnection(connectionString);
    }

    /// <summary>
    /// Crea un comando ya vinculado a una conexión con transacción iniciada.
    /// </summary>
    public static MySqlCommand CreateTransactionCommand(string conexion)
    {
        var command = new MySqlCommand
        {
            Connection = CreateConnection(conexion),
        };
        command.Transaction = command.Connection.BeginTransaction();
        return command;
    }
}
