namespace UniCore.Api.Database;

/// <summary>
/// Fábrica de transacciones inyectable, para cuando el alcance de la transacción
/// no coincide con el del consumidor (por ejemplo, trabajos en segundo plano).
/// </summary>
public sealed class DatabaseTransactionProvider
{
    private readonly DatabaseProvider _database;

    public DatabaseTransactionProvider(DatabaseProvider database)
    {
        _database = database;
    }

    public DatabaseTransaction Begin(string? conexion = null) => _database.BeginTransaction(conexion);
}
