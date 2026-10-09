using UniCore.Api.Database;

namespace UniCore.Api.Modules.EstructuraAcademica.Managers;

/// <summary>
/// Operaciones CRUD comunes a catálogos simples de Estructura Académica.
/// Cada catálogo mantiene su propio manager y declara tabla, proyección y columnas.
/// </summary>
public abstract class CatalogoAcademicoManager<TDto, TRequest>
    where TDto : class
    where TRequest : class
{
    protected abstract string Tabla { get; }
    protected abstract string Select { get; }
    protected abstract string Orden { get; }
    protected abstract string FiltroActivo { get; }
    protected abstract string ColumnaEstado { get; }
    protected abstract bool ActualizarFechaModificacion { get; }
    protected abstract object ValorEstado(bool activo);
    protected abstract object DatosActualizacion(int cod, TRequest request, string conexion);

    public Task<List<TDto>> Listar(string conexion, bool incluirInactivos)
        => DatabaseConnection.GetMany<TDto>(conexion, $"{Select} WHERE (@incluir=1 OR {FiltroActivo}) ORDER BY {Orden};", new { incluir = incluirInactivos });

    public Task<TDto?> Obtener(int cod, string conexion)
        => DatabaseConnection.GetOne<TDto>(conexion, $"{Select} WHERE t.cod=@cod LIMIT 1;", new { cod });

    public Task<long> Crear(TRequest request, string conexion)
        => DatabaseConnection.Insert(conexion, Tabla, request);

    public Task<bool> Actualizar(int cod, TRequest request, string conexion)
        => DatabaseConnection.Update(conexion, Tabla, DatosActualizacion(cod, request, conexion), new { cod }, true);

    public async Task<bool> EstablecerActivo(int cod, bool activo, string conexion)
    {
        var updatedDay = ActualizarFechaModificacion ? ", updatedday=NOW()" : string.Empty;
        var rows = await DatabaseConnection.Execute(conexion, $"UPDATE {Tabla} SET {ColumnaEstado}=@valor{updatedDay} WHERE cod=@cod;", new { valor = ValorEstado(activo), cod });
        return rows > 0 || await Obtener(cod, conexion) is not null;
    }
}
