using MySql.Data.MySqlClient;

namespace UniCore.Api.Database;

/// <summary>
/// Sonda de disponibilidad de la base de datos.
/// No forma parte de la capa de datos: solo abre una conexión y la cierra.
/// </summary>
public sealed class PersistenceHealth
{
    /// <summary>
    /// Resultado de la sonda, en una forma que System.Text.Json puede serializar.
    /// </summary>
    public sealed class DatabaseStatus
    {
        public string Estado { get; init; } = "desconocido";

        public string? Detalle { get; init; }
    }

    /// <summary>
    /// Intenta abrir la conexión indicada, o la primera declarada en la
    /// configuración si no se especifica. Nunca lanza: el resultado describe el estado.
    /// </summary>
    public DatabaseStatus GetDatabaseStatus(string? conexion = null)
    {
        var nombre = conexion ?? AppSettingsCache.GetConnectionNames().FirstOrDefault();

        if (string.IsNullOrWhiteSpace(nombre))
            return new DatabaseStatus { Estado = "sin_conexion_configurada" };

        try
        {
            using var connection = MysqlConnectionFactory.CreateConnection(nombre);
            connection.Open();
            return new DatabaseStatus { Estado = "ok", Detalle = connection.Database };
        }
        catch (MySqlException exception)
        {
            return new DatabaseStatus { Estado = "error", Detalle = exception.Message };
        }
        catch (InvalidOperationException exception)
        {
            return new DatabaseStatus { Estado = "sin_conexion_configurada", Detalle = exception.Message };
        }
    }
}

/// <summary>
/// Utilidades estáticas de diagnóstico de la configuración de base de datos.
/// </summary>
public static class DatabaseHealth
{
    /// <summary>
    /// Nombres de las conexiones declaradas en la configuración.
    /// </summary>
    public static IEnumerable<string> GetConfiguredConnections()
        => AppSettingsCache.GetConnectionNames();
}
