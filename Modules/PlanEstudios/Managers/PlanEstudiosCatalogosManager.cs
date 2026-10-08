using UniCore.Api.Database;
using UniCore.Api.Modules.PlanEstudios.Dto;
using UniCore.Api.Modules.PlanEstudios.Requests;

namespace UniCore.Api.Modules.PlanEstudios.Managers;

public sealed class PlanEstudiosCatalogosManager(DatabaseProvider db)
{
    private sealed record TablaCatalogo(string Tabla, string Codigo, string Nombre, string Descripcion, string Activo, string Orden, int MaxCodigo, int MaxNombre);
    private static readonly IReadOnlyDictionary<string, TablaCatalogo> Catalogos = new Dictionary<string, TablaCatalogo>(StringComparer.OrdinalIgnoreCase)
    {
        ["estados-configuracion"] = new("aca_estados_configuracion", "codigo", "nombre", "descripcion", "activo", "nombre", 40, 100),
        ["estados-plan"] = new("aca_estados_plan_estudio", "codigo", "nombre", "descripcion", "activo", "nombre", 40, 100),
        ["estados-cumplimiento"] = new("aca_estados_cumplimiento", "codigo", "nombre", "descripcion", "activo", "nombre", 40, 100),
        ["tipos-asignatura"] = new("aca_tipos_asignatura", "codigo", "nombre", "descripcion", "activo", "nombre", 40, 100),
        ["tipos-prerrequisito"] = new("aca_tipos_prerrequisito", "codigo", "nombre", "descripcion", "activo", "nombre", 20, 100),
        ["tipos-componente"] = new("aca_tipos_componente", "codigo", "nombre", "descripcion", "activo", "nombre", 50, 120),
        ["comportamientos-componente"] = new("aca_comportamientos_componente", "codigo", "nombre", "descripcion", "activo", "nombre", 50, 120),
        ["tipos-requisito"] = new("aca_tipos_requisito_cocurricular", "codigo", "nombre", "descripcion", "activo", "nombre", 50, 120),
        ["comportamientos-requisito"] = new("aca_comportamientos_requisito", "codigo", "nombre", "descripcion", "activo", "nombre", 50, 120),
        ["unidades-medida"] = new("aca_unidades_medida", "codigo", "nombre", "descripcion", "activo", "nombre", 30, 100),
        ["tipos-mecanismo"] = new("aca_tipos_mecanismo_cumplimiento", "codigo", "nombre", "descripcion", "activo", "nombre", 50, 120),
        ["tipos-evidencia"] = new("aca_tipos_evidencia", "codigo", "nombre", "descripcion", "activo", "nombre", 50, 120),
        ["tipos-actividad"] = new("aca_tipos_actividad_cocurricular", "codigo", "nombre", "descripcion", "activo", "nombre", 50, 120),
    };

    public static bool EsCatalogoValido(string nombre) => Catalogos.ContainsKey(nombre);
    public static bool DatosValidos(string catalogo, CatalogoPlanRequest r)
    {
        var t = ObtenerTabla(catalogo);
        return r.codigo.Trim().Length <= t.MaxCodigo && r.nombre.Trim().Length <= t.MaxNombre;
    }
    private static TablaCatalogo ObtenerTabla(string nombre) => Catalogos.TryGetValue(nombre, out var tabla) ? tabla : throw new ArgumentException("Catálogo de Plan de Estudios no válido.");

    public Task<List<CatalogoPlanDto>> Listar(string conexion, string catalogo, bool incluirInactivos)
    {
        var t = ObtenerTabla(catalogo);
        return db.GetMany<CatalogoPlanDto>($"SELECT cod,{t.Codigo} AS codigo,{t.Nombre} AS nombre,{t.Descripcion} AS descripcion,{t.Activo} AS activo FROM {t.Tabla} WHERE (@inactivos=1 OR {t.Activo}=1) ORDER BY {t.Orden},{t.Codigo};", new { inactivos = incluirInactivos }, conexion);
    }

    public Task<CatalogoPlanDto?> Obtener(string conexion, string catalogo, int cod)
    {
        var t = ObtenerTabla(catalogo);
        return db.GetOne<CatalogoPlanDto>($"SELECT cod,{t.Codigo} AS codigo,{t.Nombre} AS nombre,{t.Descripcion} AS descripcion,{t.Activo} AS activo FROM {t.Tabla} WHERE cod=@cod LIMIT 1;", new { cod }, conexion);
    }

    public Task<long?> ExisteCodigo(string conexion, string catalogo, string codigo, int? omitir)
    {
        var t = ObtenerTabla(catalogo);
        return db.ExecuteScalar<long?>($"SELECT cod FROM {t.Tabla} WHERE {t.Codigo}=@codigo AND (@omitir IS NULL OR cod<>@omitir) LIMIT 1;", new { codigo, omitir }, conexion);
    }

    public async Task<int> Crear(string conexion, string catalogo, CatalogoPlanRequest request)
    {
        var t = ObtenerTabla(catalogo);
        var id = await db.Insert(t.Tabla, new { codigo = Normalizar(request.codigo), nombre = request.nombre.Trim(), descripcion = Limpiar(request.descripcion), activo = request.activo }, conexion);
        return checked((int)id);
    }

    public Task<bool> Actualizar(string conexion, string catalogo, int cod, CatalogoPlanRequest request)
    {
        var t = ObtenerTabla(catalogo);
        return db.Update(t.Tabla, new { codigo = Normalizar(request.codigo), nombre = request.nombre.Trim(), descripcion = Limpiar(request.descripcion), activo = request.activo }, new { cod }, true, conexion);
    }

    public async Task<bool> Estado(string conexion, string catalogo, int cod, bool activo)
    {
        var t = ObtenerTabla(catalogo);
        var n = await db.Execute($"UPDATE {t.Tabla} SET {t.Activo}=@activo WHERE cod=@cod;", new { cod, activo }, conexion);
        return n > 0 || await Obtener(conexion, catalogo, cod) is not null;
    }

    public static string Normalizar(string value) => value.Trim().ToUpperInvariant();
    private static string? Limpiar(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
