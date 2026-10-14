using UniCore.Api.Database;
using UniCore.Api.Modules.Idiomas.Dto;
using UniCore.Api.Modules.Idiomas.Requests;

namespace UniCore.Api.Modules.Idiomas.Managers;

public sealed class IdiomasManager
{
    public Task<List<CatalogoIdiomaDto>> EstadosConfiguracion(string conexion) =>
        DatabaseConnection.GetMany<CatalogoIdiomaDto>(conexion, "SELECT cod,nombre FROM aca_estados_configuracion WHERE activo=1 ORDER BY cod,nombre;");

    public Task<List<AsignaturaRutaIdiomaDto>> Asignaturas(string conexion) =>
        DatabaseConnection.GetMany<AsignaturaRutaIdiomaDto>(conexion, "SELECT cod,codigo,nombre FROM idi_idioma_asignatura ORDER BY nombre,codigo;");

    public Task<bool> Existe(string entidad, int cod, string conexion)
    {
        var tabla = entidad switch
        {
            "idiomas" => "idi_idiomas",
            "niveles" => "idi_niveles_idioma",
            "rutas" => "idi_rutas_idioma",
            "certificaciones" => "idi_certificaciones_idioma",
            _ => throw new ArgumentException("Catálogo de idiomas inválido."),
        };
        return DatabaseConnection.ExecuteScalar<bool>(conexion, $"SELECT EXISTS(SELECT 1 FROM {tabla} WHERE cod=@cod);", new { cod });
    }

    public Task<long?> ExisteCodigo(string entidad, string codigo, int? idioma, int? omitir, string conexion)
    {
        var config = entidad switch
        {
            "idiomas" => ("idi_idiomas", "codigo", (string?)null),
            "niveles" => ("idi_niveles_idioma", "codigo", (string?)null),
            "rutas" => ("idi_rutas_idioma", "codigo", "cod_idioma"),
            "certificaciones" => ("idi_certificaciones_idioma", "codigo", "cod_idioma"),
            _ => throw new ArgumentException("Catálogo de idiomas inválido."),
        };
        var condition = config.Item3 is null ? string.Empty : $" AND {config.Item3}=@idioma";
        return DatabaseConnection.ExecuteScalar<long?>(conexion, $"SELECT cod FROM {config.Item1} WHERE {config.Item2}=@codigo AND (@omitir IS NULL OR cod<>@omitir){condition} LIMIT 1;", new { codigo, idioma, omitir });
    }

    public Task<bool> ReferenciasValidas(IdiomaRequest request, string conexion) =>
        DatabaseConnection.ExecuteScalar<bool>(conexion, "SELECT EXISTS(SELECT 1 FROM aca_estados_configuracion WHERE cod=@estado AND activo=1);", new { estado = request.cod_estado });

    public Task<bool> ReferenciasValidas(NivelIdiomaRequest request, string conexion) =>
        DatabaseConnection.ExecuteScalar<bool>(conexion, "SELECT EXISTS(SELECT 1 FROM aca_estados_configuracion WHERE cod=@estado AND activo=1);", new { estado = request.cod_estado });

    public Task<bool> ReferenciasValidas(RutaIdiomaRequest request, string conexion) =>
        DatabaseConnection.ExecuteScalar<bool>(conexion, @"SELECT EXISTS (
            SELECT 1 FROM idi_idiomas i
            JOIN aca_estados_configuracion e ON e.cod=@estado AND e.activo=1
            JOIN idi_niveles_idioma n ON n.cod=@nivel
            WHERE i.cod=@idioma
        );", new { idioma = request.cod_idioma, estado = request.cod_estado, nivel = request.cod_nivel_final });

    public Task<bool> ReferenciasValidas(CertificacionIdiomaRequest request, string conexion) =>
        DatabaseConnection.ExecuteScalar<bool>(conexion, "SELECT EXISTS(SELECT 1 FROM idi_idiomas i JOIN aca_estados_configuracion e ON e.cod=@estado AND e.activo=1 WHERE i.cod=@idioma);", new { idioma = request.cod_idioma, estado = request.cod_estado });

    public Task<List<IdiomaDto>> Idiomas(string conexion, bool incluirInactivos) =>
        DatabaseConnection.GetMany<IdiomaDto>(conexion, @"SELECT i.cod,i.codigo,i.nombre,i.cod_estado,e.nombre AS estado FROM idi_idiomas i JOIN aca_estados_configuracion e ON e.cod=i.cod_estado WHERE (@all=1 OR e.codigo='ACTIVO') ORDER BY i.nombre;", new { all = incluirInactivos });

    public async Task<FormatoNivelIdiomaDto> FormatoNivel(string conexion) =>
        await DatabaseConnection.GetOne<FormatoNivelIdiomaDto>(conexion, "SELECT formato_nivel FROM idi_configuracion WHERE cod=1;")
        ?? new FormatoNivelIdiomaDto();

    public Task<int> ActualizarFormatoNivel(string conexion, string formato) =>
        DatabaseConnection.Execute(conexion, "INSERT INTO idi_configuracion (cod,formato_nivel) VALUES (1,@formato) ON DUPLICATE KEY UPDATE formato_nivel=@formato;", new { formato });

    public async Task<List<NivelIdiomaDto>> Niveles(string conexion, bool incluirInactivos)
    {
        var formato = (await FormatoNivel(conexion)).formato_nivel;
        var items = await DatabaseConnection.GetMany<NivelIdiomaDto>(conexion, @"SELECT n.cod,n.codigo,n.orden,n.cod_estado,e.nombre AS estado FROM idi_niveles_idioma n JOIN aca_estados_configuracion e ON e.cod=n.cod_estado WHERE (@all=1 OR e.codigo='ACTIVO') ORDER BY n.orden;", new { all = incluirInactivos });
        foreach (var item in items) item.nombre = NombreNivel(item.orden, formato);
        return items;
    }

    public async Task<List<RutaIdiomaDto>> Rutas(string conexion, int? idioma, bool incluirInactivos)
    {
        var items = await DatabaseConnection.GetMany<RutaIdiomaDto>(conexion, @"SELECT r.cod,r.cod_idioma,i.nombre AS idioma,r.codigo,r.nombre,r.descripcion,r.cod_nivel_final,r.cod_estado,e.nombre AS estado FROM idi_rutas_idioma r JOIN idi_idiomas i ON i.cod=r.cod_idioma JOIN aca_estados_configuracion e ON e.cod=r.cod_estado WHERE (@idioma IS NULL OR r.cod_idioma=@idioma) AND (@all=1 OR e.codigo='ACTIVO') ORDER BY i.nombre,r.nombre;", new { idioma, all = incluirInactivos });
        var ids = items.Select(item => item.cod).ToArray();
        if (ids.Length == 0) return items;
        var formato = (await FormatoNivel(conexion)).formato_nivel;
        var asignaturas = await DatabaseConnection.GetMany<RutaIdiomaAsignaturaDto>(conexion, @"SELECT ra.cod,ra.cod_ruta_idioma,ra.cod_idioma_asignatura,a.codigo AS codigo_asignatura,a.nombre AS asignatura,ra.cod_nivel_idioma,n.orden AS nivel_orden,a.descripcion,a.creditos,a.horas_teoricas,a.horas_practicas,ra.orden,ra.cod_estado FROM idi_ruta_idioma_asignaturas ra JOIN idi_idioma_asignatura a ON a.cod=ra.cod_idioma_asignatura LEFT JOIN idi_niveles_idioma n ON n.cod=ra.cod_nivel_idioma WHERE ra.cod_ruta_idioma IN @ids ORDER BY ra.cod_ruta_idioma,ra.orden;", new { ids });
        foreach (var asignatura in asignaturas)
        {
            if (asignatura.nivel_orden.HasValue)
            {
                asignatura.nivel_idioma = NombreNivel(asignatura.nivel_orden.Value, formato);
                asignatura.asignatura = $"{items.First(item => item.cod == asignatura.cod_ruta_idioma).idioma} {asignatura.nivel_idioma}";
            }
        }
        foreach (var item in items) item.asignaturas = asignaturas.Where(asignatura => asignatura.cod_ruta_idioma == item.cod).ToList();
        return items;
    }

    public async Task<List<CertificacionIdiomaDto>> Certificaciones(string conexion, int? idioma, bool incluirInactivos)
    {
        var items = await DatabaseConnection.GetMany<CertificacionIdiomaDto>(conexion, @"SELECT ci.cod,ci.cod_idioma,i.nombre AS idioma,ci.codigo,ci.nombre,ci.entidad_emisora,ci.descripcion,ci.vigencia_meses,ci.cod_estado,e.nombre AS estado FROM idi_certificaciones_idioma ci JOIN idi_idiomas i ON i.cod=ci.cod_idioma JOIN aca_estados_configuracion e ON e.cod=ci.cod_estado WHERE (@idioma IS NULL OR ci.cod_idioma=@idioma) AND (@all=1 OR e.codigo='ACTIVO') ORDER BY i.nombre,ci.nombre;", new { idioma, all = incluirInactivos });
        var ids = items.Select(item => item.cod).ToArray();
        if (ids.Length == 0) return items;
        var formato = (await FormatoNivel(conexion)).formato_nivel;
        var niveles = await DatabaseConnection.GetMany<CertificacionNivelDto>(conexion, @"SELECT cn.cod,cn.cod_certificacion,cn.cod_nivel_idioma,n.orden AS nivel_orden,cn.puntaje_minimo,cn.puntaje_maximo FROM idi_certificacion_idioma_niveles cn JOIN idi_niveles_idioma n ON n.cod=cn.cod_nivel_idioma WHERE cn.cod_certificacion IN @ids ORDER BY cn.cod_certificacion,n.orden;", new { ids });
        foreach (var nivel in niveles) nivel.nivel = NombreNivel(nivel.nivel_orden, formato);
        foreach (var item in items) item.niveles = niveles.Where(nivel => nivel.cod_certificacion == item.cod).ToList();
        return items;
    }

    public async Task<int> CrearIdioma(string conexion, IdiomaRequest request) =>
        checked((int)await DatabaseConnection.Insert(conexion, "idi_idiomas", new { codigo = Normalizar(request.codigo), nombre = request.nombre.Trim(), request.cod_estado }));

    public Task<bool> ActualizarIdioma(string conexion, int cod, IdiomaRequest request) =>
        DatabaseConnection.Update(conexion, "idi_idiomas", new { codigo = Normalizar(request.codigo), nombre = request.nombre.Trim(), request.cod_estado }, new { cod }, true);

    public async Task<int> CrearNivel(string conexion, NivelIdiomaRequest request)
    {
        await using var tx = DatabaseConnection.BeginTransaction(conexion);
        var ultimoOrden = await DatabaseConnection.ExecuteScalarTransaccion<int?>(tx, "SELECT orden FROM idi_niveles_idioma ORDER BY orden DESC LIMIT 1 FOR UPDATE;");
        var orden = (ultimoOrden ?? 0) + 1;
        var codigo = $"NIVEL-{orden:D2}";
        var cod = checked((int)await DatabaseConnection.InsertTransaccion(tx, "idi_niveles_idioma", new { codigo, orden, request.cod_estado }));
        tx.Commit();
        return cod;
    }

    public Task<bool> ActualizarNivel(string conexion, int cod, NivelIdiomaRequest request) =>
        DatabaseConnection.Update(conexion, "idi_niveles_idioma", new { request.cod_estado }, new { cod }, true);

    public async Task<int> CrearRuta(string conexion, RutaIdiomaRequest request) =>
        checked((int)await DatabaseConnection.Insert(conexion, "idi_rutas_idioma", new { request.cod_idioma, codigo = Normalizar(request.codigo), nombre = request.nombre.Trim(), descripcion = Limpiar(request.descripcion), request.cod_nivel_final, request.cod_estado }));

    public Task<bool> ActualizarRuta(string conexion, int cod, RutaIdiomaRequest request) =>
        DatabaseConnection.Update(conexion, "idi_rutas_idioma", new { request.cod_idioma, codigo = Normalizar(request.codigo), nombre = request.nombre.Trim(), descripcion = Limpiar(request.descripcion), request.cod_nivel_final, request.cod_estado }, new { cod }, true);

    public async Task<int> CrearCertificacion(string conexion, CertificacionIdiomaRequest request) =>
        checked((int)await DatabaseConnection.Insert(conexion, "idi_certificaciones_idioma", new { request.cod_idioma, codigo = Normalizar(request.codigo), nombre = request.nombre.Trim(), entidad_emisora = Limpiar(request.entidad_emisora), descripcion = Limpiar(request.descripcion), request.vigencia_meses, request.cod_estado }));

    public Task<bool> ActualizarCertificacion(string conexion, int cod, CertificacionIdiomaRequest request) =>
        DatabaseConnection.Update(conexion, "idi_certificaciones_idioma", new { request.cod_idioma, codigo = Normalizar(request.codigo), nombre = request.nombre.Trim(), entidad_emisora = Limpiar(request.entidad_emisora), descripcion = Limpiar(request.descripcion), request.vigencia_meses, request.cod_estado }, new { cod }, true);

    public async Task ReemplazarAsignaturasRuta(string conexion, int ruta, IReadOnlyCollection<RutaAsignaturaRequest> items)
    {
        await using var tx = DatabaseConnection.BeginTransaction(conexion);
        var contexto = await DatabaseConnection.GetOneTransaccion<RutaIdiomaContexto>(tx, @"SELECT r.cod,r.cod_nivel_final,i.nombre AS idioma,n.orden AS orden_final,c.formato_nivel FROM idi_rutas_idioma r JOIN idi_idiomas i ON i.cod=r.cod_idioma JOIN idi_niveles_idioma n ON n.cod=r.cod_nivel_final CROSS JOIN idi_configuracion c WHERE r.cod=@ruta AND c.cod=1;", new { ruta });
        if (contexto is null) throw new InvalidOperationException("La ruta no existe o no tiene configuración de idiomas.");

        var niveles = await DatabaseConnection.GetManyTransaccion<NivelIdiomaDto>(tx, "SELECT cod,codigo,orden,cod_estado,'' AS estado FROM idi_niveles_idioma WHERE orden<=@orden AND cod_estado IN (SELECT cod FROM aca_estados_configuracion WHERE codigo='ACTIVO') ORDER BY orden;", new { orden = contexto.orden_final });
        if (items.Count != niveles.Count || items.Select(item => item.cod_nivel_idioma).Distinct().Count() != items.Count || !items.Select(item => item.cod_nivel_idioma).OrderBy(cod => cod).SequenceEqual(niveles.Select(nivel => nivel.cod).OrderBy(cod => cod)))
            throw new InvalidOperationException("La ruta debe incluir exactamente una fila por cada nivel hasta el nivel final.");
        if (items.Select(item => item.codigo.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != items.Count)
            throw new InvalidOperationException("No se pueden repetir códigos de asignatura dentro de la ruta.");
        if (items.Any(item => string.IsNullOrWhiteSpace(item.codigo)))
            throw new InvalidOperationException("El código es obligatorio para cada asignatura de la ruta.");

        var estados = items.Select(item => item.cod_estado).Distinct().ToArray();
        if (estados.Length > 0 && (await DatabaseConnection.GetManyTransaccion<int>(tx, "SELECT cod FROM aca_estados_configuracion WHERE cod IN @ids AND activo=1;", new { ids = estados })).Count != estados.Length)
            throw new InvalidOperationException("Un estado de asignatura de ruta no existe o está inactivo.");

        var existentes = await DatabaseConnection.GetManyTransaccion<RutaAsignaturaExistente>(tx, "SELECT cod,cod_nivel_idioma,cod_idioma_asignatura FROM idi_ruta_idioma_asignaturas WHERE cod_ruta_idioma=@ruta;", new { ruta });
        var nivelIds = items.Select(item => item.cod_nivel_idioma).ToArray();
        var removidos = existentes.Where(actual => !nivelIds.Contains(actual.cod_nivel_idioma)).ToArray();
        foreach (var actual in removidos)
        {
            await DatabaseConnection.DeleteTransaccion(tx, "idi_ruta_idioma_asignaturas", new { cod = actual.cod });
            await DatabaseConnection.ExecuteTransaccion(tx, "DELETE FROM idi_idioma_asignatura WHERE cod=@cod AND NOT EXISTS (SELECT 1 FROM idi_ruta_idioma_asignaturas WHERE cod_idioma_asignatura=@cod);", new { cod = actual.cod_idioma_asignatura });
        }

        for (var index = 0; index < items.Count; index++)
        {
            var item = items.ElementAt(index);
            var nivel = niveles.Single(value => value.cod == item.cod_nivel_idioma);
            var nombre = $"{contexto.idioma} {NombreNivel(nivel.orden, contexto.formato_nivel)}";
            var codigo = Normalizar(item.codigo);
            var existente = existentes.FirstOrDefault(actual => actual.cod_nivel_idioma == item.cod_nivel_idioma);
            long codAsignatura;
            if (existente is not null)
            {
                codAsignatura = existente.cod_idioma_asignatura;
                await DatabaseConnection.ExecuteTransaccion(tx, "UPDATE idi_idioma_asignatura SET codigo=@codigo,nombre=@nombre,descripcion=@descripcion,creditos=@creditos,horas_teoricas=@horas_teoricas,horas_practicas=@horas_practicas WHERE cod=@cod;", new { codigo, nombre, descripcion = Limpiar(item.descripcion), item.creditos, item.horas_teoricas, item.horas_practicas, cod = codAsignatura });
                await DatabaseConnection.ExecuteTransaccion(tx, "UPDATE idi_ruta_idioma_asignaturas SET orden=@orden,cod_estado=@cod_estado WHERE cod=@cod;", new { orden = index + 1, item.cod_estado, cod = existente.cod });
            }
            else
            {
                codAsignatura = await DatabaseConnection.InsertTransaccion(tx, "idi_idioma_asignatura", new { codigo, nombre, descripcion = Limpiar(item.descripcion), item.creditos, item.horas_teoricas, item.horas_practicas });
                await DatabaseConnection.InsertTransaccion(tx, "idi_ruta_idioma_asignaturas", new { cod_ruta_idioma = ruta, cod_idioma_asignatura = codAsignatura, cod_nivel_idioma = item.cod_nivel_idioma, orden = index + 1, item.cod_estado });
            }
        }
        tx.Commit();
    }

    public async Task ReemplazarNivelesCertificacion(string conexion, int certificacion, IReadOnlyCollection<CertificacionNivelRequest> items)
    {
        if (items.Select(item => item.cod_nivel_idioma).Distinct().Count() != items.Count)
            throw new InvalidOperationException("La certificación no puede repetir niveles.");
        if (items.Any(item => item.puntaje_minimo.HasValue && item.puntaje_maximo.HasValue && item.puntaje_minimo > item.puntaje_maximo))
            throw new InvalidOperationException("El puntaje mínimo no puede superar el máximo.");
        await using var tx = DatabaseConnection.BeginTransaction(conexion);
        var niveles = items.Select(item => item.cod_nivel_idioma).ToArray();
        if (niveles.Length > 0 && (await DatabaseConnection.GetManyTransaccion<int>(tx, "SELECT cod FROM idi_niveles_idioma WHERE cod IN @ids;", new { ids = niveles })).Count != niveles.Length)
            throw new InvalidOperationException("Un nivel indicado no existe.");
        if (!await DatabaseConnection.ExecuteScalarTransaccion<bool>(tx, "SELECT EXISTS(SELECT 1 FROM idi_certificaciones_idioma WHERE cod=@certificacion);", new { certificacion }))
            throw new InvalidOperationException("La certificación no existe.");
        await DatabaseConnection.DeleteTransaccion(tx, "idi_certificacion_idioma_niveles", new { cod_certificacion = certificacion });
        foreach (var item in items)
            await DatabaseConnection.InsertTransaccion(tx, "idi_certificacion_idioma_niveles", new { cod_certificacion = certificacion, cod_nivel_idioma = item.cod_nivel_idioma, item.puntaje_minimo, item.puntaje_maximo });
        tx.Commit();
    }

    private static string Normalizar(string codigo) => codigo.Trim().ToUpperInvariant();
    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    public static string NombreNivel(int orden, string formato)
    {
        if (formato == "ARABIGO") return orden.ToString();
        var numeros = new (int valor, string romano)[] { (1000,"M"),(900,"CM"),(500,"D"),(400,"CD"),(100,"C"),(90,"XC"),(50,"L"),(40,"XL"),(10,"X"),(9,"IX"),(5,"V"),(4,"IV"),(1,"I") };
        var resultado = string.Empty;
        foreach (var (valor, romano) in numeros)
            while (orden >= valor) { resultado += romano; orden -= valor; }
        return resultado;
    }

    private sealed class RutaIdiomaContexto
    {
        public int cod { get; set; }
        public int? cod_nivel_final { get; set; }
        public string idioma { get; set; } = string.Empty;
        public int orden_final { get; set; }
        public string formato_nivel { get; set; } = "ROMANO";
    }

    private sealed class RutaAsignaturaExistente
    {
        public long cod { get; set; }
        public int cod_nivel_idioma { get; set; }
        public long cod_idioma_asignatura { get; set; }
    }
}
