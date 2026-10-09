using UniCore.Api.Database;
using UniCore.Api.Modules.PlanEstudios.Dto;
using UniCore.Api.Modules.PlanEstudios.Requests;

namespace UniCore.Api.Modules.PlanEstudios.Managers;

public sealed class CatalogosRelacionadosManager
{
    public Task<bool> Existe(string entidad, int cod, string c)
    {
        var tabla = entidad switch { "idiomas" => "aca_idiomas", "niveles" => "aca_niveles_idioma", "rutas" => "aca_rutas_idioma", "certificaciones" => "aca_certificaciones_idioma", "actividades" => "aca_actividades_cocurriculares", _ => throw new ArgumentException("Catálogo relacional inválido.") };
        return DatabaseConnection.ExecuteScalar<bool>(c, $"SELECT EXISTS(SELECT 1 FROM {tabla} WHERE cod=@cod);", new { cod });
    }
    public Task<bool> ReferenciasValidas(IdiomaRequest r, string c) => DatabaseConnection.ExecuteScalar<bool>(c, "SELECT EXISTS(SELECT 1 FROM aca_estados_configuracion WHERE cod=@estado AND activo=1);", new { estado = r.cod_estado });
    public Task<bool> ReferenciasValidas(NivelIdiomaRequest r, string c) => DatabaseConnection.ExecuteScalar<bool>(c, "SELECT EXISTS(SELECT 1 FROM aca_estados_configuracion WHERE cod=@estado AND activo=1);", new { estado = r.cod_estado });
    public Task<bool> ReferenciasValidas(RutaIdiomaRequest r, string c) => DatabaseConnection.ExecuteScalar<bool>(
        c,
        @"SELECT EXISTS (
            SELECT 1
            FROM aca_idiomas i
            JOIN aca_estados_configuracion e ON e.cod = @estado AND e.activo = 1
            LEFT JOIN aca_niveles_idioma n ON n.cod = @nivel
            WHERE i.cod = @idioma
              AND (@nivel IS NULL OR n.cod IS NOT NULL)
        );",
        new { idioma = r.cod_idioma, estado = r.cod_estado, nivel = r.cod_nivel_final });
    public Task<bool> ReferenciasValidas(CertificacionIdiomaRequest r, string c) => DatabaseConnection.ExecuteScalar<bool>(c, "SELECT EXISTS(SELECT 1 FROM aca_idiomas i JOIN aca_estados_configuracion e ON e.cod=@estado AND e.activo=1 WHERE i.cod=@idioma);", new { idioma = r.cod_idioma, estado = r.cod_estado });
    public Task<bool> ReferenciasValidas(ActividadCocurricularRequest r, string c) => DatabaseConnection.ExecuteScalar<bool>(c, "SELECT EXISTS(SELECT 1 FROM aca_tipos_actividad_cocurricular t JOIN aca_estados_configuracion e ON e.cod=@estado AND e.activo=1 WHERE t.cod=@tipo AND t.activo=1);", new { tipo = r.cod_tipo_actividad, estado = r.cod_estado });
    public Task<List<IdiomaDto>> Idiomas(string c, bool inactivos) => DatabaseConnection.GetMany<IdiomaDto>(c, @"SELECT i.cod,i.codigo,i.nombre,i.cod_estado,e.nombre AS estado FROM aca_idiomas i JOIN aca_estados_configuracion e ON e.cod=i.cod_estado WHERE (@all=1 OR e.codigo='ACTIVO') ORDER BY i.nombre;", new { all = inactivos });
    public Task<List<NivelIdiomaDto>> Niveles(string c, bool inactivos) => DatabaseConnection.GetMany<NivelIdiomaDto>(c, @"SELECT n.cod,n.codigo,n.nombre,n.orden,n.cod_estado,e.nombre AS estado FROM aca_niveles_idioma n JOIN aca_estados_configuracion e ON e.cod=n.cod_estado WHERE (@all=1 OR e.codigo='ACTIVO') ORDER BY n.orden,n.nombre;", new { all = inactivos });
    public async Task<List<RutaIdiomaDto>> Rutas(string c, int? idioma, bool inactivos)
    {
        var items = await DatabaseConnection.GetMany<RutaIdiomaDto>(c, @"SELECT r.cod,r.cod_idioma,i.nombre AS idioma,r.codigo,r.nombre,r.descripcion,r.cod_nivel_final,r.cod_estado,e.nombre AS estado FROM aca_rutas_idioma r JOIN aca_idiomas i ON i.cod=r.cod_idioma JOIN aca_estados_configuracion e ON e.cod=r.cod_estado WHERE (@idioma IS NULL OR r.cod_idioma=@idioma) AND (@all=1 OR e.codigo='ACTIVO') ORDER BY i.nombre,r.nombre;", new { idioma, all = inactivos });
        var ids = items.Select(x => x.cod).ToArray(); if (ids.Length == 0) return items;
        var asigs = await DatabaseConnection.GetMany<RutaIdiomaAsignaturaDto>(c, @"SELECT ra.cod,ra.cod_ruta_idioma,ra.cod_asignatura,a.codigo AS codigo_asignatura,a.nombre AS asignatura,ra.cod_nivel_idioma,n.nombre AS nivel_idioma,ra.orden,ra.cod_estado FROM aca_ruta_idioma_asignaturas ra JOIN aca_asignaturas a ON a.cod=ra.cod_asignatura LEFT JOIN aca_niveles_idioma n ON n.cod=ra.cod_nivel_idioma WHERE ra.cod_ruta_idioma IN @ids ORDER BY ra.cod_ruta_idioma,ra.orden;", new { ids });
        foreach (var item in items) item.asignaturas = asigs.Where(a => a.cod_ruta_idioma == item.cod).ToList();
        return items;
    }
    public async Task<List<CertificacionIdiomaDto>> Certificaciones(string c, int? idioma, bool inactivos)
    {
        var items = await DatabaseConnection.GetMany<CertificacionIdiomaDto>(c, @"SELECT ci.cod,ci.cod_idioma,i.nombre AS idioma,ci.codigo,ci.nombre,ci.entidad_emisora,ci.descripcion,ci.vigencia_meses,ci.cod_estado,e.nombre AS estado FROM aca_certificaciones_idioma ci JOIN aca_idiomas i ON i.cod=ci.cod_idioma JOIN aca_estados_configuracion e ON e.cod=ci.cod_estado WHERE (@idioma IS NULL OR ci.cod_idioma=@idioma) AND (@all=1 OR e.codigo='ACTIVO') ORDER BY i.nombre,ci.nombre;", new { idioma, all = inactivos });
        var ids = items.Select(x => x.cod).ToArray(); if (ids.Length == 0) return items;
        var niveles = await DatabaseConnection.GetMany<CertificacionNivelDto>(c, @"SELECT cn.cod,cn.cod_certificacion,cn.cod_nivel_idioma,n.nombre AS nivel,cn.puntaje_minimo,cn.puntaje_maximo FROM aca_certificacion_idioma_niveles cn JOIN aca_niveles_idioma n ON n.cod=cn.cod_nivel_idioma WHERE cn.cod_certificacion IN @ids ORDER BY cn.cod_certificacion,n.orden;", new { ids });
        foreach (var item in items) item.niveles = niveles.Where(n => n.cod_certificacion == item.cod).ToList();
        return items;
    }
    public Task<List<ActividadCocurricularDto>> Actividades(string c, bool inactivos) => DatabaseConnection.GetMany<ActividadCocurricularDto>(c, @"SELECT a.cod,a.codigo,a.nombre,a.descripcion,a.cod_tipo_actividad,t.nombre AS tipo_actividad,a.cod_estado,e.nombre AS estado FROM aca_actividades_cocurriculares a JOIN aca_tipos_actividad_cocurricular t ON t.cod=a.cod_tipo_actividad JOIN aca_estados_configuracion e ON e.cod=a.cod_estado WHERE (@all=1 OR e.codigo='ACTIVO') ORDER BY a.nombre;", new { all = inactivos });

    public Task<long?> ExisteCodigo(string tabla, string codigo, int? idioma, int? omitir, string c)
    {
        var config = tabla switch { "idiomas" => ("aca_idiomas", "codigo", (string?)null), "niveles" => ("aca_niveles_idioma", "codigo", (string?)null), "rutas" => ("aca_rutas_idioma", "codigo", "cod_idioma"), "certificaciones" => ("aca_certificaciones_idioma", "codigo", "cod_idioma"), "actividades" => ("aca_actividades_cocurriculares", "codigo", (string?)null), _ => throw new ArgumentException("Catálogo relacional inválido.") };
        var condition = config.Item3 is null ? "" : $" AND {config.Item3}=@idioma";
        return DatabaseConnection.ExecuteScalar<long?>(c, $"SELECT cod FROM {config.Item1} WHERE {config.Item2}=@codigo AND (@omitir IS NULL OR cod<>@omitir){condition} LIMIT 1;", new { codigo, idioma, omitir });
    }
    public async Task<int> CrearIdioma(string c, IdiomaRequest r) => checked((int)await DatabaseConnection.Insert(c, "aca_idiomas", new { codigo = Up(r.codigo), nombre = r.nombre.Trim(), r.cod_estado }));
    public Task<bool> ActualizarIdioma(string c, int cod, IdiomaRequest r) => DatabaseConnection.Update(c, "aca_idiomas", new { codigo = Up(r.codigo), nombre = r.nombre.Trim(), r.cod_estado }, new { cod }, true);
    public async Task<int> CrearNivel(string c, NivelIdiomaRequest r) => checked((int)await DatabaseConnection.Insert(c, "aca_niveles_idioma", new { codigo = Up(r.codigo), nombre = r.nombre.Trim(), r.orden, r.cod_estado }));
    public Task<bool> ActualizarNivel(string c, int cod, NivelIdiomaRequest r) => DatabaseConnection.Update(c, "aca_niveles_idioma", new { codigo = Up(r.codigo), nombre = r.nombre.Trim(), r.orden, r.cod_estado }, new { cod }, true);
    public async Task<int> CrearRuta(string c, RutaIdiomaRequest r) => checked((int)await DatabaseConnection.Insert(c, "aca_rutas_idioma", new { r.cod_idioma, codigo = Up(r.codigo), nombre = r.nombre.Trim(), descripcion = Clean(r.descripcion), r.cod_nivel_final, r.cod_estado }));
    public Task<bool> ActualizarRuta(string c, int cod, RutaIdiomaRequest r) => DatabaseConnection.Update(c, "aca_rutas_idioma", new { r.cod_idioma, codigo = Up(r.codigo), nombre = r.nombre.Trim(), descripcion = Clean(r.descripcion), r.cod_nivel_final, r.cod_estado }, new { cod }, true);
    public async Task<int> CrearCertificacion(string c, CertificacionIdiomaRequest r) => checked((int)await DatabaseConnection.Insert(c, "aca_certificaciones_idioma", new { r.cod_idioma, codigo = Up(r.codigo), nombre = r.nombre.Trim(), entidad_emisora = Clean(r.entidad_emisora), descripcion = Clean(r.descripcion), r.vigencia_meses, r.cod_estado }));
    public Task<bool> ActualizarCertificacion(string c, int cod, CertificacionIdiomaRequest r) => DatabaseConnection.Update(c, "aca_certificaciones_idioma", new { r.cod_idioma, codigo = Up(r.codigo), nombre = r.nombre.Trim(), entidad_emisora = Clean(r.entidad_emisora), descripcion = Clean(r.descripcion), r.vigencia_meses, r.cod_estado }, new { cod }, true);
    public async Task<int> CrearActividad(string c, ActividadCocurricularRequest r) => checked((int)await DatabaseConnection.Insert(c, "aca_actividades_cocurriculares", new { codigo = Up(r.codigo), nombre = r.nombre.Trim(), descripcion = Clean(r.descripcion), r.cod_tipo_actividad, r.cod_estado }));
    public async Task<bool> ActualizarActividad(string c, int cod, ActividadCocurricularRequest r)
        => await DatabaseConnection.Execute(c, "UPDATE aca_actividades_cocurriculares SET codigo=@codigo,nombre=@nombre,descripcion=@descripcion,cod_tipo_actividad=@cod_tipo_actividad,cod_estado=@cod_estado,updatedday=NOW() WHERE cod=@cod;", new { cod, codigo = Up(r.codigo), nombre = r.nombre.Trim(), descripcion = Clean(r.descripcion), r.cod_tipo_actividad, r.cod_estado }) > 0;

    public async Task ReemplazarAsignaturasRuta(string c, int ruta, IReadOnlyCollection<RutaAsignaturaRequest> items)
    {
        if (items.Select(i => i.cod_asignatura).Distinct().Count() != items.Count) throw new InvalidOperationException("La ruta no puede repetir asignaturas.");
        await using var tx = DatabaseConnection.BeginTransaction(c);
        if (!await DatabaseConnection.ExecuteScalarTransaccion<bool>(tx, "SELECT EXISTS(SELECT 1 FROM aca_rutas_idioma WHERE cod=@ruta);", new { ruta })) throw new InvalidOperationException("La ruta no existe.");
        var asig = items.Select(i => i.cod_asignatura).Distinct().ToArray();
        if (asig.Length > 0 && (await DatabaseConnection.GetManyTransaccion<int>(tx, "SELECT cod FROM aca_asignaturas WHERE cod IN @ids;", new { ids = asig })).Count != asig.Length) throw new InvalidOperationException("Una asignatura indicada no existe.");
        var estados = items.Select(i => i.cod_estado).Distinct().ToArray();
        if (estados.Length > 0 && (await DatabaseConnection.GetManyTransaccion<int>(tx, "SELECT cod FROM aca_estados_configuracion WHERE cod IN @ids AND activo=1;", new { ids = estados })).Count != estados.Length) throw new InvalidOperationException("Un estado de asignatura de ruta no existe o está inactivo.");
        var niveles = items.Where(i => i.cod_nivel_idioma.HasValue).Select(i => i.cod_nivel_idioma!.Value).Distinct().ToArray();
        if (niveles.Length > 0 && (await DatabaseConnection.GetManyTransaccion<int>(tx, "SELECT cod FROM aca_niveles_idioma WHERE cod IN @ids;", new { ids = niveles })).Count != niveles.Length) throw new InvalidOperationException("Un nivel de idioma indicado no existe.");
        await DatabaseConnection.ExecuteTransaccion(tx, "DELETE FROM aca_ruta_idioma_asignaturas WHERE cod_ruta_idioma=@ruta;", new { ruta });
        foreach (var i in items) await DatabaseConnection.ExecuteTransaccion(tx, "INSERT INTO aca_ruta_idioma_asignaturas(cod_ruta_idioma,cod_asignatura,cod_nivel_idioma,orden,cod_estado) VALUES(@ruta,@asignatura,@nivel,@orden,@estado);", new { ruta, asignatura = i.cod_asignatura, nivel = i.cod_nivel_idioma, i.orden, estado = i.cod_estado });
        tx.Commit();
    }
    public async Task ReemplazarNivelesCertificacion(string c, int cert, IReadOnlyCollection<CertificacionNivelRequest> items)
    {
        if (items.Select(i => i.cod_nivel_idioma).Distinct().Count() != items.Count) throw new InvalidOperationException("La certificación no puede repetir niveles.");
        if (items.Any(i => i.puntaje_minimo.HasValue && i.puntaje_maximo.HasValue && i.puntaje_minimo > i.puntaje_maximo)) throw new InvalidOperationException("El puntaje mínimo no puede superar el máximo.");
        await using var tx = DatabaseConnection.BeginTransaction(c);
        var ids = items.Select(i => i.cod_nivel_idioma).ToArray();
        if (ids.Length > 0 && (await DatabaseConnection.GetManyTransaccion<int>(tx, "SELECT cod FROM aca_niveles_idioma WHERE cod IN @ids;", new { ids })).Count != ids.Length) throw new InvalidOperationException("Un nivel indicado no existe.");
        if (!await DatabaseConnection.ExecuteScalarTransaccion<bool>(tx, "SELECT EXISTS(SELECT 1 FROM aca_certificaciones_idioma WHERE cod=@cert);", new { cert })) throw new InvalidOperationException("La certificación no existe.");
        await DatabaseConnection.ExecuteTransaccion(tx, "DELETE FROM aca_certificacion_idioma_niveles WHERE cod_certificacion=@cert;", new { cert });
        foreach (var i in items) await DatabaseConnection.ExecuteTransaccion(tx, "INSERT INTO aca_certificacion_idioma_niveles(cod_certificacion,cod_nivel_idioma,puntaje_minimo,puntaje_maximo) VALUES(@cert,@nivel,@min,@max);", new { cert, nivel = i.cod_nivel_idioma, min = i.puntaje_minimo, max = i.puntaje_maximo });
        tx.Commit();
    }
    public async Task<bool> Estado(string tabla, int cod, int estado, string c)
    {
        var config = tabla switch { "idiomas" => ("aca_idiomas", "cod_estado"), "niveles" => ("aca_niveles_idioma", "cod_estado"), "rutas" => ("aca_rutas_idioma", "cod_estado"), "certificaciones" => ("aca_certificaciones_idioma", "cod_estado"), "actividades" => ("aca_actividades_cocurriculares", "cod_estado"), _ => throw new ArgumentException("Catálogo relacional inválido.") };
        if (!await DatabaseConnection.ExecuteScalar<bool>(c, "SELECT EXISTS(SELECT 1 FROM aca_estados_configuracion WHERE cod=@estado AND activo=1);", new { estado })) return false;
        var updated = await DatabaseConnection.Execute(c, $"UPDATE {config.Item1} SET {config.Item2}=@estado WHERE cod=@cod;", new { cod, estado });
        return updated > 0 || await Existe(tabla, cod, c);
    }
    private static string Up(string s) => s.Trim().ToUpperInvariant();
    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
