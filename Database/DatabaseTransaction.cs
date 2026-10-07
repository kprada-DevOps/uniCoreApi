using MySql.Data.MySqlClient;

namespace UniCore.Api.Database;

/// <summary>
/// Unidad transaccional explícita sobre MySQL. Si se descarta sin Commit se hace rollback defensivo.
/// </summary>
public sealed class DatabaseTransaction : IDisposable, IAsyncDisposable
{
    private bool _done;

    public DatabaseTransaction(string conexion)
    {
        Connection = MysqlConnectionFactory.CreateConnection(conexion);
        try
        {
            Connection.Open();
            Transaction = Connection.BeginTransaction();
        }
        catch
        {
            Connection.Dispose();
            throw;
        }
    }

    public MySqlConnection Connection { get; }

    public MySqlTransaction Transaction { get; }

    public void Commit()
    {
        Transaction.Commit();
        _done = true;
    }

    public void Rollback()
    {
        Transaction.Rollback();
        _done = true;
    }

    public void Dispose()
    {
        if (!_done) SafeRollback();
        Transaction?.Dispose();
        Connection?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (!_done) SafeRollback();
        if (Transaction != null) await Transaction.DisposeAsync();
        if (Connection != null) await Connection.DisposeAsync();
    }

    private void SafeRollback()
    {
        try { Transaction?.Rollback(); }
        catch { }
    }
}
