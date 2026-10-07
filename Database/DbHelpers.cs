using System.Globalization;

namespace UniCore.Api.Database;

/// <summary>
/// Utilidades genéricas usadas por <see cref="DatabaseConnection"/>.
/// Sustituye al helper general del proyecto de referencia, del que solo se conservan
/// los tres métodos que la capa de datos realmente necesita.
/// </summary>
public static class DbHelpers
{
    /// <summary>
    /// Genera una secuencia de <paramref name="count"/> enteros correlativos desde <paramref name="start"/>.
    /// </summary>
    public static IEnumerable<long> CreateRange(long start, long count)
    {
        for (var i = 0; i < count; i++)
            yield return start + i;
    }

    /// <summary>
    /// Aplana por reflexión varios objetos en un único diccionario de parámetros.
    /// Si hay claves duplicadas, gana el último valor.
    /// </summary>
    public static object ConcatObjects(List<object?> objects)
    {
        var result = new Dictionary<string, object?>();
        foreach (var item in objects)
        {
            if (item == null) continue;
            foreach (var property in item.GetType().GetProperties())
            {
                if (!property.CanRead) continue;
                result[property.Name] = property.GetValue(item);
            }
        }

        return result;
    }

    /// <summary>
    /// Fecha y hora actual ajustada a la zona horaria configurada para la conexión,
    /// con formato "yyyy-MM-dd HH:mm:ss".
    /// </summary>
    public static string GetFechaActual(string conexion)
    {
        return GetFechaActualDatetime(conexion).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    public static DateTime GetFechaActualDatetime(string conexion)
    {
        var zonaHoraria = AppSettingsCache.GetZonaHoraria(conexion);
        if (string.IsNullOrWhiteSpace(zonaHoraria))
            return DateTime.UtcNow;

        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(zonaHoraria);
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        }
        catch (TimeZoneNotFoundException)
        {
            return DateTime.UtcNow;
        }
        catch (InvalidTimeZoneException)
        {
            return DateTime.UtcNow;
        }
    }
}
