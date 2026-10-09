using UniCore.Api.Database;
using UniCore.Api.Modules.PlanEstudios.Dto;
using UniCore.Api.Modules.PlanEstudios.Requests;

namespace UniCore.Api.Modules.PlanEstudios.Managers;

public sealed class RequisitosCocurricularesManager
{
    private const string Select = @"SELECT r.cod,r.codigo,r.nombre,r.descripcion,r.cod_tipo_requisito,t.nombre AS tipo_requisito,r.cod_comportamiento,b.nombre AS comportamiento,r.cantidad,r.cod_unidad_medida,u.nombre AS unidad_medida,r.cod_estado,e.nombre AS estado FROM aca_requisitos_cocurriculares r JOIN aca_tipos_requisito_cocurricular t ON t.cod=r.cod_tipo_requisito JOIN aca_comportamientos_requisito b ON b.cod=r.cod_comportamiento LEFT JOIN aca_unidades_medida u ON u.cod=r.cod_unidad_medida JOIN aca_estados_configuracion e ON e.cod=r.cod_estado";
    public async Task<List<RequisitoCocurricularDto>> Listar(string conexion, bool incluirInactivos, string? busqueda)
    {
        var rows = await DatabaseConnection.GetMany<RequisitoCocurricularDto>(conexion, $"{Select} WHERE (@inactivos=1 OR e.codigo='ACTIVO') AND (@filtro IS NULL OR r.codigo LIKE @filtro OR r.nombre LIKE @filtro) ORDER BY r.nombre,r.codigo;", new { inactivos = incluirInactivos, filtro = string.IsNullOrWhiteSpace(busqueda) ? null : $"%{busqueda.Trim()}%" });
        await CargarRelaciones(conexion, rows);
        return rows;
    }
    public async Task<RequisitoCocurricularDto?> Obtener(string conexion, int cod)
    {
        var item = await DatabaseConnection.GetOne<RequisitoCocurricularDto>(conexion, $"{Select} WHERE r.cod=@cod LIMIT 1;", new { cod });
        if (item is not null) await CargarRelaciones(conexion, [item]);
        return item;
    }
    public Task<long?> ExisteCodigo(string conexion, string codigo, int? omitir) => DatabaseConnection.ExecuteScalar<long?>(conexion, "SELECT cod FROM aca_requisitos_cocurriculares WHERE codigo=@codigo AND (@omitir IS NULL OR cod<>@omitir) LIMIT 1;", new { codigo, omitir });
    public Task<bool> ReferenciasValidas(string conexion, RequisitoCocurricularRequest r) => DatabaseConnection.ExecuteScalar<bool>(conexion, @"SELECT EXISTS(SELECT 1 FROM aca_tipos_requisito_cocurricular t JOIN aca_comportamientos_requisito b ON b.cod=@comportamiento AND b.activo=1 JOIN aca_estados_configuracion e ON e.cod=@estado AND e.activo=1 LEFT JOIN aca_unidades_medida u ON u.cod=@unidad AND u.activo=1 WHERE t.cod=@tipo AND t.activo=1 AND (@unidad IS NULL OR u.cod IS NOT NULL));", new { tipo = r.cod_tipo_requisito, comportamiento = r.cod_comportamiento, estado = r.cod_estado, unidad = r.cod_unidad_medida });
    public Task<bool> EstadoExiste(string conexion, int estado) => DatabaseConnection.ExecuteScalar<bool>(conexion, "SELECT EXISTS(SELECT 1 FROM aca_estados_configuracion WHERE cod=@estado AND activo=1);", new { estado });
    public async Task<int> Crear(string conexion, RequisitoCocurricularRequest r)
    {
        var id = await DatabaseConnection.Insert(conexion, "aca_requisitos_cocurriculares", Param(r)); return checked((int)id);
    }
    public async Task<bool> Actualizar(string conexion, int cod, RequisitoCocurricularRequest r)
        => await DatabaseConnection.Execute(conexion, "UPDATE aca_requisitos_cocurriculares SET codigo=@codigo,nombre=@nombre,descripcion=@descripcion,cod_tipo_requisito=@cod_tipo_requisito,cod_comportamiento=@cod_comportamiento,cantidad=@cantidad,cod_unidad_medida=@cod_unidad_medida,cod_estado=@cod_estado,updatedday=NOW() WHERE cod=@cod;", new { cod, codigo = r.codigo.Trim().ToUpperInvariant(), nombre = r.nombre.Trim(), descripcion = Limpio(r.descripcion), r.cod_tipo_requisito, r.cod_comportamiento, r.cantidad, r.cod_unidad_medida, r.cod_estado }) > 0;
    public async Task<bool> CambiarEstado(string conexion, int cod, int estado)
        => await DatabaseConnection.Execute(conexion, "UPDATE aca_requisitos_cocurriculares SET cod_estado=@estado,updatedday=NOW() WHERE cod=@cod;", new { cod, estado }) > 0 || await Obtener(conexion, cod) is not null;

    public async Task ReemplazarMecanismos(string conexion, int requisito, IReadOnlyCollection<MecanismoRequisitoRequest> items)
    {
        var tipos = items.Select(x => x.cod_tipo_mecanismo).ToArray();
        if (tipos.Distinct().Count() != tipos.Length) throw new InvalidOperationException("No se puede repetir un mecanismo en el requisito.");
        if (items.Any(x => x.rutas.Distinct().Count() != x.rutas.Count)) throw new InvalidOperationException("Un mecanismo no puede repetir rutas.");
        await using var tx = DatabaseConnection.BeginTransaction(conexion);
        if (tipos.Length > 0 && (await DatabaseConnection.GetManyTransaccion<int>(tx, "SELECT cod FROM aca_tipos_mecanismo_cumplimiento WHERE cod IN @ids AND activo=1;", new { ids = tipos })).Count != tipos.Length)
            throw new InvalidOperationException("Un tipo de mecanismo no existe o está inactivo.");
        var estados = items.Select(x => x.cod_estado).Distinct().ToArray();
        if (estados.Length > 0 && (await DatabaseConnection.GetManyTransaccion<int>(tx, "SELECT cod FROM aca_estados_configuracion WHERE cod IN @ids AND activo=1;", new { ids = estados })).Count != estados.Length)
            throw new InvalidOperationException("Un estado de mecanismo no existe o está inactivo.");
        var rutas = items.SelectMany(x => x.rutas).Distinct().ToArray();
        if (rutas.Length > 0 && (await DatabaseConnection.GetManyTransaccion<int>(tx, "SELECT cod FROM aca_rutas_idioma WHERE cod IN @ids;", new { ids = rutas })).Count != rutas.Length)
            throw new InvalidOperationException("Una ruta asociada al mecanismo no existe.");
        var existentes = await DatabaseConnection.GetManyTransaccion<MecanismoPersistido>(tx, "SELECT cod,cod_tipo_mecanismo FROM aca_requisito_mecanismos WHERE cod_requisito=@requisito;", new { requisito });
        foreach (var i in items)
        {
            var existente = existentes.FirstOrDefault(x => x.cod_tipo_mecanismo == i.cod_tipo_mecanismo);
            long cod;
            if (existente is null)
                cod = await DatabaseConnection.ExecuteScalarTransaccion<long>(tx, "INSERT INTO aca_requisito_mecanismos(cod_requisito,cod_tipo_mecanismo,nombre,descripcion,obligatorio,cod_estado) VALUES(@requisito,@tipo,@nombre,@descripcion,@obligatorio,@estado); SELECT LAST_INSERT_ID();", new { requisito, tipo = i.cod_tipo_mecanismo, nombre = Limpio(i.nombre), descripcion = Limpio(i.descripcion), i.obligatorio, estado = i.cod_estado });
            else
            {
                cod = existente.cod;
                await DatabaseConnection.ExecuteTransaccion(tx, "UPDATE aca_requisito_mecanismos SET nombre=@nombre,descripcion=@descripcion,obligatorio=@obligatorio,cod_estado=@estado WHERE cod=@cod;", new { cod, nombre = Limpio(i.nombre), descripcion = Limpio(i.descripcion), i.obligatorio, estado = i.cod_estado });
                await DatabaseConnection.ExecuteTransaccion(tx, "DELETE FROM aca_requisito_mecanismo_rutas WHERE cod_requisito_mecanismo=@cod;", new { cod });
            }
            foreach (var ruta in i.rutas.Distinct()) await DatabaseConnection.ExecuteTransaccion(tx, "INSERT INTO aca_requisito_mecanismo_rutas(cod_requisito_mecanismo,cod_ruta_idioma) VALUES(@mecanismo,@ruta);", new { mecanismo = cod, ruta });
        }
        var omitidos = existentes.Where(x => !tipos.Contains(x.cod_tipo_mecanismo)).ToArray();
        foreach (var omitido in omitidos)
        {
            var tieneEvidencia = await DatabaseConnection.ExecuteScalarTransaccion<bool>(tx, "SELECT EXISTS(SELECT 1 FROM aca_estudiante_requisito_evidencias WHERE cod_requisito_mecanismo=@cod);", new { cod = omitido.cod });
            if (tieneEvidencia)
            {
                var inactivo = await DatabaseConnection.ExecuteScalarTransaccion<int?>(tx, "SELECT cod FROM aca_estados_configuracion WHERE codigo='INACTIVO' AND activo=1 LIMIT 1;");
                if (!inactivo.HasValue) throw new InvalidOperationException("No se puede retirar un mecanismo con evidencias porque falta el estado INACTIVO.");
                await DatabaseConnection.ExecuteTransaccion(tx, "UPDATE aca_requisito_mecanismos SET cod_estado=@estado WHERE cod=@cod;", new { estado = inactivo.Value, cod = omitido.cod });
            }
            else
            {
                await DatabaseConnection.ExecuteTransaccion(tx, "DELETE FROM aca_requisito_mecanismo_rutas WHERE cod_requisito_mecanismo=@cod;", new { cod = omitido.cod });
                await DatabaseConnection.ExecuteTransaccion(tx, "DELETE FROM aca_requisito_mecanismos WHERE cod=@cod;", new { cod = omitido.cod });
            }
        }
        tx.Commit();
    }

    public async Task ReemplazarIdiomas(string conexion, int requisito, IReadOnlyCollection<RequisitoIdiomaRequest> items)
    {
        var tipo = await DatabaseConnection.ExecuteScalar<string>(conexion, "SELECT t.codigo FROM aca_requisitos_cocurriculares r JOIN aca_tipos_requisito_cocurricular t ON t.cod=r.cod_tipo_requisito WHERE r.cod=@requisito;", new { requisito });
        if (!string.Equals(tipo, "IDIOMA", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Solo los requisitos de tipo IDIOMA pueden tener configuración de idiomas.");
        var idiomas = items.Select(i => i.cod_idioma).ToArray();
        if (idiomas.Distinct().Count() != idiomas.Length) throw new InvalidOperationException("No se puede asociar dos veces el mismo idioma al requisito.");
        await using var tx = DatabaseConnection.BeginTransaction(conexion);
        foreach (var item in items)
        {
            var ok = await DatabaseConnection.ExecuteScalarTransaccion<bool>(tx, @"SELECT EXISTS(SELECT 1 FROM aca_idiomas i JOIN aca_niveles_idioma n ON n.cod=@nivel AND n.cod_estado IN (SELECT cod FROM aca_estados_configuracion WHERE codigo='ACTIVO') WHERE i.cod=@idioma AND i.cod_estado IN (SELECT cod FROM aca_estados_configuracion WHERE codigo='ACTIVO'));", new { idioma = item.cod_idioma, nivel = item.cod_nivel_minimo });
            if (!ok) throw new InvalidOperationException("El idioma o nivel mínimo no existe o está inactivo.");
            if (item.rutas.Distinct().Count() != item.rutas.Count || item.certificaciones.Distinct().Count() != item.certificaciones.Count) throw new InvalidOperationException("Hay rutas o certificaciones duplicadas.");
            if (item.rutas.Count > 0 && await DatabaseConnection.ExecuteScalarTransaccion<int>(tx, "SELECT COUNT(*) FROM aca_rutas_idioma WHERE cod IN @ids AND cod_idioma=@idioma AND cod_estado IN (SELECT cod FROM aca_estados_configuracion WHERE codigo='ACTIVO');", new { ids = item.rutas, idioma = item.cod_idioma }) != item.rutas.Distinct().Count()) throw new InvalidOperationException("Una ruta no pertenece al idioma seleccionado o está inactiva.");
            if (item.certificaciones.Count > 0 && await DatabaseConnection.ExecuteScalarTransaccion<int>(tx, "SELECT COUNT(*) FROM aca_certificaciones_idioma WHERE cod IN @ids AND cod_idioma=@idioma AND cod_estado IN (SELECT cod FROM aca_estados_configuracion WHERE codigo='ACTIVO');", new { ids = item.certificaciones, idioma = item.cod_idioma }) != item.certificaciones.Distinct().Count()) throw new InvalidOperationException("Una certificación no pertenece al idioma seleccionado o está inactiva.");
        }
        await DatabaseConnection.ExecuteTransaccion(tx, "DELETE FROM aca_requisito_idioma_rutas WHERE cod_requisito_idioma IN (SELECT cod FROM aca_requisito_idiomas WHERE cod_requisito=@requisito);", new { requisito });
        await DatabaseConnection.ExecuteTransaccion(tx, "DELETE FROM aca_requisito_idioma_certificaciones WHERE cod_requisito_idioma IN (SELECT cod FROM aca_requisito_idiomas WHERE cod_requisito=@requisito);", new { requisito });
        await DatabaseConnection.ExecuteTransaccion(tx, "DELETE FROM aca_requisito_idiomas WHERE cod_requisito=@requisito;", new { requisito });
        foreach (var item in items)
        {
            var cod = await DatabaseConnection.ExecuteScalarTransaccion<long>(tx, "INSERT INTO aca_requisito_idiomas(cod_requisito,cod_idioma,cod_nivel_minimo) VALUES(@requisito,@idioma,@nivel); SELECT LAST_INSERT_ID();", new { requisito, idioma = item.cod_idioma, nivel = item.cod_nivel_minimo });
            foreach (var ruta in item.rutas.Distinct()) await DatabaseConnection.ExecuteTransaccion(tx, "INSERT INTO aca_requisito_idioma_rutas(cod_requisito_idioma,cod_ruta_idioma) VALUES(@cod,@ruta);", new { cod, ruta });
            foreach (var cert in item.certificaciones.Distinct()) await DatabaseConnection.ExecuteTransaccion(tx, "INSERT INTO aca_requisito_idioma_certificaciones(cod_requisito_idioma,cod_certificacion) VALUES(@cod,@cert);", new { cod, cert });
        }
        tx.Commit();
    }

    public async Task ReemplazarActividades(string conexion, int requisito, IReadOnlyCollection<int> actividades)
    {
        var tipo = await DatabaseConnection.ExecuteScalar<string>(conexion, "SELECT t.codigo FROM aca_requisitos_cocurriculares r JOIN aca_tipos_requisito_cocurricular t ON t.cod=r.cod_tipo_requisito WHERE r.cod=@requisito;", new { requisito });
        if (!string.Equals(tipo, "CULTURA_DEPORTE", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Solo los requisitos de tipo CULTURA_DEPORTE pueden tener actividades.");
        if (actividades.Distinct().Count() != actividades.Count) throw new InvalidOperationException("No se permiten actividades duplicadas.");
        await using var tx = DatabaseConnection.BeginTransaction(conexion);
        if (actividades.Count > 0 && (await DatabaseConnection.GetManyTransaccion<int>(tx, "SELECT cod FROM aca_actividades_cocurriculares WHERE cod IN @ids AND cod_estado IN (SELECT cod FROM aca_estados_configuracion WHERE codigo='ACTIVO');", new { ids = actividades })).Count != actividades.Count)
            throw new InvalidOperationException("Una actividad indicada no existe.");
        await DatabaseConnection.ExecuteTransaccion(tx, "DELETE FROM aca_requisito_actividades WHERE cod_requisito=@requisito;", new { requisito });
        foreach (var actividad in actividades) await DatabaseConnection.ExecuteTransaccion(tx, "INSERT INTO aca_requisito_actividades(cod_requisito,cod_actividad) VALUES(@requisito,@actividad);", new { requisito, actividad });
        tx.Commit();
    }

    private async Task CargarRelaciones(string conexion, List<RequisitoCocurricularDto> rows)
    {
        var ids = rows.Select(r => r.cod).ToArray(); if (ids.Length == 0) return;
        var mecanismos = await DatabaseConnection.GetMany<RequisitoMecanismoDto>(conexion, @"SELECT m.cod,m.cod_requisito,m.cod_tipo_mecanismo,t.nombre AS tipo_mecanismo,m.nombre,m.descripcion,m.obligatorio,m.cod_estado FROM aca_requisito_mecanismos m JOIN aca_tipos_mecanismo_cumplimiento t ON t.cod=m.cod_tipo_mecanismo WHERE m.cod_requisito IN @ids ORDER BY m.cod_requisito,m.cod;", new { ids });
        var idiomas = await DatabaseConnection.GetMany<RequisitoIdiomaDto>(conexion, @"SELECT ri.cod,ri.cod_requisito,ri.cod_idioma,i.nombre AS idioma,ri.cod_nivel_minimo,n.nombre AS nivel_minimo FROM aca_requisito_idiomas ri JOIN aca_idiomas i ON i.cod=ri.cod_idioma JOIN aca_niveles_idioma n ON n.cod=ri.cod_nivel_minimo WHERE ri.cod_requisito IN @ids ORDER BY ri.cod_requisito,i.nombre;", new { ids });
        var actividades = await DatabaseConnection.GetMany<RequisitoActividadDto>(conexion, @"SELECT ra.cod_requisito,ac.cod AS cod_actividad,ac.codigo,ac.nombre,ac.cod_tipo_actividad FROM aca_requisito_actividades ra JOIN aca_actividades_cocurriculares ac ON ac.cod=ra.cod_actividad WHERE ra.cod_requisito IN @ids ORDER BY ra.cod_requisito,ac.nombre;", new { ids });
        var riIds = idiomas.Select(i => i.cod).ToArray();
        var mecanismoIds = mecanismos.Select(m => m.cod).ToArray();
        var mecanismoRutas = mecanismoIds.Length == 0 ? [] : await DatabaseConnection.GetMany<RequisitoMecanismoRutaFila>(conexion, "SELECT cod_requisito_mecanismo,cod_ruta_idioma FROM aca_requisito_mecanismo_rutas WHERE cod_requisito_mecanismo IN @ids;", new { ids = mecanismoIds });
        foreach (var mecanismo in mecanismos) mecanismo.rutas = mecanismoRutas.Where(x => x.cod_requisito_mecanismo == mecanismo.cod).Select(x => x.cod_ruta_idioma).ToList();
        var rutas = riIds.Length == 0 ? [] : await DatabaseConnection.GetMany<RequisitoIdiomaRutaFila>(conexion, "SELECT cod_requisito_idioma,cod_ruta_idioma FROM aca_requisito_idioma_rutas WHERE cod_requisito_idioma IN @ids;", new { ids = riIds });
        var certificaciones = riIds.Length == 0 ? [] : await DatabaseConnection.GetMany<RequisitoIdiomaCertificacionFila>(conexion, "SELECT cod_requisito_idioma,cod_certificacion FROM aca_requisito_idioma_certificaciones WHERE cod_requisito_idioma IN @ids;", new { ids = riIds });
        foreach (var idioma in idiomas)
        {
            idioma.rutas = rutas.Where(x => x.cod_requisito_idioma == idioma.cod).Select(x => x.cod_ruta_idioma).ToList();
            idioma.certificaciones = certificaciones.Where(x => x.cod_requisito_idioma == idioma.cod).Select(x => x.cod_certificacion).ToList();
        }
        foreach (var r in rows)
        {
            r.mecanismos = mecanismos.Where(m => m.cod_requisito == r.cod).ToList();
            r.idiomas = idiomas.Where(i => i.cod_requisito == r.cod).ToList();
            r.actividades = actividades.Where(a => a.cod_requisito == r.cod).ToList();
        }
    }
    private static object Param(RequisitoCocurricularRequest r) => new { codigo = r.codigo.Trim().ToUpperInvariant(), nombre = r.nombre.Trim(), descripcion = Limpio(r.descripcion), r.cod_tipo_requisito, r.cod_comportamiento, r.cantidad, r.cod_unidad_medida, r.cod_estado };
    private static string? Limpio(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private sealed class RequisitoIdiomaRutaFila { public long cod_requisito_idioma { get; set; } public int cod_ruta_idioma { get; set; } }
    private sealed class RequisitoIdiomaCertificacionFila { public long cod_requisito_idioma { get; set; } public int cod_certificacion { get; set; } }
    private sealed class RequisitoMecanismoRutaFila { public long cod_requisito_mecanismo { get; set; } public int cod_ruta_idioma { get; set; } }
    private sealed class MecanismoPersistido { public long cod { get; set; } public int cod_tipo_mecanismo { get; set; } }
}
