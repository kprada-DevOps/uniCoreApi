using UniCore.Api.Database;

namespace UniCore.Api.Modules.EstructuraAcademica.Managers;

/// <summary>
/// Operaciones CRUD comunes a catálogos simples de Estructura Académica.
/// Cada catálogo mantiene su propio manager y declara tabla, proyección y columnas.
/// </summary>
public abstract class CatalogoAcademicoManager<TDto, TRequest>(DatabaseProvider database)
    where TDto : class
    where TRequest : class
{
    protected readonly DatabaseProvider Database = database;
    protected abstract string Tabla { get; }
    protected abstract string Select { get; }
    protected abstract string Orden { get; }
    protected abstract string FiltroActivo { get; }
    protected abstract string ColumnaEstado { get; }
    protected abstract bool ActualizarFechaModificacion { get; }
    protected abstract object ValorEstado(bool activo);
    protected abstract object DatosActualizacion(int cod, TRequest request, string conexion);

    public Task<List<TDto>> Listar(string conexion, bool incluirInactivos)
        => Database.GetMany<TDto>(
            $"{Select} WHERE (@incluir=1 OR {FiltroActivo}) ORDER BY {Orden};",
            new { incluir = incluirInactivos }, conexion);

    public Task<TDto?> Obtener(int cod, string conexion)
        => Database.GetOne<TDto>($"{Select} WHERE t.cod=@cod LIMIT 1;", new { cod }, conexion);

    public Task<long> Crear(TRequest request, string conexion)
        => Database.Insert(Tabla, request, conexion);

    public Task<bool> Actualizar(int cod, TRequest request, string conexion)
        => Database.Update(Tabla, DatosActualizacion(cod, request, conexion), new { cod }, true, conexion);

    public async Task<bool> EstablecerActivo(int cod, bool activo, string conexion)
    {
        var updatedDay = ActualizarFechaModificacion ? ", updatedday=NOW()" : string.Empty;
        var rows = await Database.Execute(
            $"UPDATE {Tabla} SET {ColumnaEstado}=@valor{updatedDay} WHERE cod=@cod;",
            new { valor = ValorEstado(activo), cod }, conexion);
        return rows > 0 || await Obtener(cod, conexion) is not null;
    }
}
