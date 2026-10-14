using System.Globalization;
using UniCore.Api.Database;
using UniCore.Api.Modules.PlanEstudios.Dto;
using UniCore.Api.Modules.PlanEstudios.Requests;

namespace UniCore.Api.Modules.PlanEstudios.Managers;

public sealed class AsignaturasManager
{
    private const string Select = @"SELECT a.cod,a.codigo,a.nombre,a.descripcion,a.creditos,a.horas_teoricas,a.horas_practicas,a.cod_tipo_asignatura,t.nombre AS tipo_asignatura,a.cod_estado,e.nombre AS estado,a.createdday,a.updatedday FROM aca_asignaturas a JOIN aca_tipos_asignatura t ON t.cod=a.cod_tipo_asignatura JOIN aca_estados_configuracion e ON e.cod=a.cod_estado";
    public async Task<List<AsignaturaDto>> Listar(string conexion, bool incluirInactivas, string? busqueda)
    {
        var asignaturas = await DatabaseConnection.GetMany<AsignaturaDto>(
            conexion,
            $"{Select} WHERE (@inactivos=1 OR e.codigo='ACTIVO') AND (@busqueda IS NULL OR a.codigo LIKE @filtro OR a.nombre LIKE @filtro) ORDER BY a.nombre,a.codigo;",
            new
            {
                inactivos = incluirInactivas,
                busqueda,
                filtro = string.IsNullOrWhiteSpace(busqueda) ? null : $"%{busqueda.Trim()}%",
            });
        if (asignaturas.Count == 0) return asignaturas;

        var codigos = asignaturas.Select(asignatura => asignatura.cod).ToArray();
        var prerrequisitos = await DatabaseConnection.GetMany<PrerrequisitoDto>(
            conexion,
            """
            SELECT p.cod_asignatura,a.codigo AS codigo_asignatura,a.nombre AS nombre_asignatura,
                   p.cod_asignatura_requisito,r.codigo AS codigo_requisito,r.nombre AS nombre_requisito,
                   p.cod_tipo_prerrequisito,t.nombre AS tipo_prerrequisito
            FROM aca_asignatura_prerrequisitos p
            JOIN aca_asignaturas a ON a.cod=p.cod_asignatura
            JOIN aca_asignaturas r ON r.cod=p.cod_asignatura_requisito
            JOIN aca_tipos_prerrequisito t ON t.cod=p.cod_tipo_prerrequisito
            WHERE p.cod_asignatura IN @codigos
            ORDER BY p.cod_asignatura,r.nombre;
            """,
            new { codigos });
        var porAsignatura = prerrequisitos
            .GroupBy(prerrequisito => prerrequisito.cod_asignatura)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.ToList());

        foreach (var asignatura in asignaturas)
            if (porAsignatura.TryGetValue(asignatura.cod, out var requisitos))
                asignatura.prerrequisitos = requisitos;

        return asignaturas;
    }
    public Task<AsignaturaDto?> Obtener(string conexion, int cod) => DatabaseConnection.GetOne<AsignaturaDto>(conexion, $"{Select} WHERE a.cod=@cod LIMIT 1;", new { cod });
    public Task<long?> ExisteCodigo(string conexion, string codigo, int? omitir) => DatabaseConnection.ExecuteScalar<long?>(conexion, "SELECT cod FROM aca_asignaturas WHERE codigo=@codigo AND (@omitir IS NULL OR cod<>@omitir) LIMIT 1;", new { codigo, omitir });
    public Task<bool> TipoExiste(string conexion, int cod) => DatabaseConnection.ExecuteScalar<bool>(conexion, "SELECT EXISTS(SELECT 1 FROM aca_tipos_asignatura WHERE cod=@cod AND activo=1);", new { cod });
    public Task<bool> EstadoExiste(string conexion, int cod) => DatabaseConnection.ExecuteScalar<bool>(conexion, "SELECT EXISTS(SELECT 1 FROM aca_estados_configuracion WHERE cod=@cod AND activo=1);", new { cod });
    public async Task<bool> ReferenciasValidas(string conexion, IReadOnlyCollection<AsignaturaRequest> asignaturas)
    {
        var tipos = asignaturas.Select(x => x.cod_tipo_asignatura).Distinct().ToArray();
        var estados = asignaturas.Select(x => x.cod_estado).Distinct().ToArray();
        var tiposValidos = await DatabaseConnection.GetMany<int>(conexion, "SELECT cod FROM aca_tipos_asignatura WHERE activo=1 AND cod IN @codigos;", new { codigos = tipos });
        if (tiposValidos.Count != tipos.Length) return false;
        var estadosValidos = await DatabaseConnection.GetMany<int>(conexion, "SELECT cod FROM aca_estados_configuracion WHERE activo=1 AND cod IN @codigos;", new { codigos = estados });
        return estadosValidos.Count == estados.Length;
    }

    public async Task<List<AsignaturaSecuencialPreviewDto>> PrevisualizarSecuencia(
        string conexion,
        PrevisualizarAsignaturasSecuencialesRequest request)
    {
        var prefijo = request.prefijo.Trim().ToUpperInvariant();
        var nombreBase = request.nombre_base.Trim();
        if (string.IsNullOrEmpty(prefijo) || prefijo.Any(c => !char.IsAsciiLetterOrDigit(c)))
            throw new ArgumentException("El prefijo debe contener únicamente letras y números.");
        if (string.IsNullOrEmpty(nombreBase))
            throw new ArgumentException("El nombre base es obligatorio.");
        if (request.cantidad is < 1 or > 50)
            throw new ArgumentException("La cantidad debe estar entre 1 y 50.");

        var existentes = await DatabaseConnection.GetMany<string>(
            conexion,
            "SELECT codigo FROM aca_asignaturas WHERE codigo LIKE @patron;",
            new { patron = prefijo + "%" });
        var mayorConsecutivo = existentes
            .Select(codigo => ConsecutivoDeCodigo(codigo, prefijo))
            .Where(consecutivo => consecutivo.HasValue)
            .Select(consecutivo => consecutivo!.Value)
            .DefaultIfEmpty(0)
            .Max();

        var inicio = mayorConsecutivo + 1;
        var fin = inicio + request.cantidad - 1;
        var ancho = Math.Max(2, fin.ToString(CultureInfo.InvariantCulture).Length);
        var resultado = new List<AsignaturaSecuencialPreviewDto>(request.cantidad);

        for (var indice = 0; indice < request.cantidad; indice++)
        {
            var consecutivo = inicio + indice;
            var codigo = prefijo + consecutivo.ToString($"D{ancho}", CultureInfo.InvariantCulture);
            var nombre = $"{nombreBase} {ConvertirARomano(indice + 1)}";
            if (codigo.Length > 40)
                throw new ArgumentException("El prefijo es demasiado largo para formar códigos de hasta 40 caracteres.");
            if (nombre.Length > 180)
                throw new ArgumentException("El nombre generado supera los 180 caracteres permitidos.");
            resultado.Add(new AsignaturaSecuencialPreviewDto { codigo = codigo, nombre = nombre });
        }

        return resultado;
    }

    public async Task<List<int>> CrearMasivas(DatabaseTransaction transaccion, IReadOnlyCollection<AsignaturaRequest> asignaturas)
    {
        var codigos = asignaturas.Select(x => x.codigo.Trim().ToUpperInvariant()).ToArray();
        var duplicados = codigos
            .GroupBy(codigo => codigo, StringComparer.OrdinalIgnoreCase)
            .Where(grupo => grupo.Count() > 1)
            .Select(grupo => grupo.Key)
            .ToArray();
        if (duplicados.Length > 0) throw new AsignaturasDuplicadasException(duplicados);

        var existentes = await DatabaseConnection.GetManyTransaccion<string>(
            transaccion,
            "SELECT codigo FROM aca_asignaturas WHERE codigo IN @codigos;",
            new { codigos });
        var duplicadosExistentes = existentes
            .Select(codigo => codigo.Trim().ToUpperInvariant())
            .Where(codigo => codigos.Contains(codigo, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (duplicadosExistentes.Length > 0) throw new AsignaturasDuplicadasException(duplicadosExistentes);

        var ids = new List<int>(asignaturas.Count);
        foreach (var asignatura in asignaturas)
        {
            var id = await DatabaseConnection.InsertTransaccion(transaccion, "aca_asignaturas", Param(asignatura));
            ids.Add(checked((int)id));
        }
        return ids;
    }

    public async Task<List<AsignaturaDto>> ObtenerPorCodigos(string conexion, IReadOnlyCollection<int> codigos)
    {
        if (codigos.Count == 0) return [];
        return await DatabaseConnection.GetMany<AsignaturaDto>(conexion, $"{Select} WHERE a.cod IN @codigos;", new { codigos });
    }
    public async Task<int> Crear(string conexion, AsignaturaRequest r)
    {
        var id = await DatabaseConnection.Insert(conexion, "aca_asignaturas", Param(r));
        return checked((int)id);
    }
    public async Task<bool> Actualizar(string conexion, int cod, AsignaturaRequest r)
        => await DatabaseConnection.Update(conexion, "aca_asignaturas", new { cod, codigo = r.codigo.Trim().ToUpperInvariant(), nombre = r.nombre.Trim(), descripcion = Limpio(r.descripcion), r.creditos, r.horas_teoricas, r.horas_practicas, r.cod_tipo_asignatura, r.cod_estado, updatedday = DbHelpers.GetFechaActualDatetime(conexion) });
    public async Task<bool> CambiarEstado(string conexion, int cod, int estado)
        => await DatabaseConnection.Update(conexion, "aca_asignaturas", new { cod, cod_estado = estado, updatedday = DbHelpers.GetFechaActualDatetime(conexion) }) || await Obtener(conexion, cod) is not null;
    public Task<List<PrerrequisitoDto>> Prerrequisitos(string conexion, int asignatura)
        => DatabaseConnection.GetMany<PrerrequisitoDto>(conexion, @"SELECT p.cod_asignatura,a.codigo AS codigo_asignatura,a.nombre AS nombre_asignatura,p.cod_asignatura_requisito,r.codigo AS codigo_requisito,r.nombre AS nombre_requisito,p.cod_tipo_prerrequisito,t.nombre AS tipo_prerrequisito FROM aca_asignatura_prerrequisitos p JOIN aca_asignaturas a ON a.cod=p.cod_asignatura JOIN aca_asignaturas r ON r.cod=p.cod_asignatura_requisito JOIN aca_tipos_prerrequisito t ON t.cod=p.cod_tipo_prerrequisito WHERE p.cod_asignatura=@asignatura ORDER BY r.nombre;", new { asignatura });
    public Task<bool> AgregarPrerrequisito(string conexion, int asignatura, PrerrequisitoRequest r)
        => AgregarPrerrequisitos(
            conexion,
            asignatura,
            [r.cod_asignatura_requisito],
            r.cod_tipo_prerrequisito);

    public async Task<bool> AgregarPrerrequisitos(
        string conexion,
        int asignatura,
        IReadOnlyCollection<int> requisitos,
        int tipo)
    {
        var codigosRequisito = requisitos.Distinct().Order().ToArray();
        if (codigosRequisito.Length is < 1 or > 50 || codigosRequisito.Length != requisitos.Count)
            return false;
        if (codigosRequisito.Contains(asignatura))
            throw new InvalidOperationException("Una asignatura no puede ser su propio prerrequisito.");

        await using var tx = DatabaseConnection.BeginTransaction(conexion);
        var codigos = codigosRequisito.Append(asignatura).Distinct().Order().ToArray();
        var asignaturas = await DatabaseConnection.GetManyTransaccion<int>(
            tx,
            "SELECT cod FROM aca_asignaturas WHERE cod IN @codigos ORDER BY cod FOR UPDATE;",
            new { codigos });
        if (asignaturas.Count != codigos.Length) return false;
        if (!await DatabaseConnection.ExecuteScalarTransaccion<bool>(
                tx,
                "SELECT EXISTS(SELECT 1 FROM aca_tipos_prerrequisito WHERE cod=@tipo AND activo=1);",
                new { tipo }))
            return false;

        var duplicados = await DatabaseConnection.GetManyTransaccion<int>(
            tx,
            "SELECT cod_asignatura_requisito FROM aca_asignatura_prerrequisitos WHERE cod_asignatura=@asignatura AND cod_asignatura_requisito IN @requisitos;",
            new { asignatura, requisitos = codigosRequisito });
        if (duplicados.Count > 0) return false;

        var edges = await DatabaseConnection.GetManyTransaccion<PrerequisiteEdge>(tx, "SELECT cod_asignatura,cod_asignatura_requisito FROM aca_asignatura_prerrequisitos;");
        var graph = edges
            .GroupBy(x => x.cod_asignatura)
            .ToDictionary(g => g.Key, g => g.Select(x => x.cod_asignatura_requisito).ToHashSet());

        foreach (var requisito in codigosRequisito)
        {
            var pending = new Stack<int>();
            var visitados = new HashSet<int>();
            pending.Push(requisito);
            while (pending.TryPop(out var actual))
            {
                if (actual == asignatura)
                    throw new InvalidOperationException("La relación crearía un ciclo de prerrequisitos.");
                if (!visitados.Add(actual) || !graph.TryGetValue(actual, out var siguientes))
                    continue;
                foreach (var siguiente in siguientes) pending.Push(siguiente);
            }

            if (!graph.TryGetValue(asignatura, out var prerequisitosExistentes))
            {
                prerequisitosExistentes = [];
                graph[asignatura] = prerequisitosExistentes;
            }
            prerequisitosExistentes.Add(requisito);
        }

        await DatabaseConnection.InsertManyTransaccion(
            tx,
            "aca_asignatura_prerrequisitos",
            codigosRequisito.Select(requisito => new
            {
                cod_asignatura = asignatura,
                cod_asignatura_requisito = requisito,
                cod_tipo_prerrequisito = tipo,
            }));
        tx.Commit();
        return true;
    }

    public async Task<List<string>?> EliminarSiNoRelacionada(DatabaseTransaction transaccion, int cod)
    {
        var existe = await DatabaseConnection.GetOneTransaccion<int>(
            transaccion,
            "SELECT cod FROM aca_asignaturas WHERE cod=@cod FOR UPDATE;",
            new { cod });
        if (existe == 0) return null;

        var dependencias = await DatabaseConnection.GetManyTransaccion<DependenciaAsignatura>(
            transaccion,
            """
            SELECT relacion, cantidad
            FROM (
                SELECT 'Prerrequisitos de otras asignaturas' AS relacion, COUNT(*) AS cantidad FROM aca_asignatura_prerrequisitos WHERE cod_asignatura_requisito=@cod
                UNION ALL SELECT 'Prerrequisitos propios' AS relacion, COUNT(*) AS cantidad FROM aca_asignatura_prerrequisitos WHERE cod_asignatura=@cod
                UNION ALL SELECT 'Opciones de componentes' AS relacion, COUNT(*) AS cantidad FROM aca_componente_opciones WHERE cod_asignatura=@cod
                UNION ALL SELECT 'Grupos académicos' AS relacion, COUNT(*) AS cantidad FROM aca_grupos WHERE cod_asignatura=@cod
                UNION ALL SELECT 'Historial académico' AS relacion, COUNT(*) AS cantidad FROM aca_historial_academico WHERE cod_asignatura=@cod
                UNION ALL SELECT 'Homologaciones (origen)' AS relacion, COUNT(*) AS cantidad FROM aca_homologaciones WHERE cod_asignatura_origen=@cod
                UNION ALL SELECT 'Homologaciones (destino)' AS relacion, COUNT(*) AS cantidad FROM aca_homologaciones WHERE cod_asignatura_destino=@cod
                UNION ALL SELECT 'Asignaciones a planes' AS relacion, COUNT(*) AS cantidad FROM aca_plan_asignaturas WHERE cod_asignatura=@cod
                UNION ALL SELECT 'Elementos de planes de estudio' AS relacion, COUNT(*) AS cantidad FROM aca_plan_elementos WHERE cod_asignatura=@cod
                UNION ALL SELECT 'Recuperaciones' AS relacion, COUNT(*) AS cantidad FROM eva_recuperaciones WHERE cod_asignatura=@cod
            ) AS referencias
            WHERE cantidad > 0;
            """,
            new { cod });

        if (dependencias.Count > 0)
            return dependencias.Select(dependencia => $"{dependencia.relacion}: {dependencia.cantidad}").ToList();

        await DatabaseConnection.DeleteTransaccion(transaccion, "aca_asignaturas", new { cod });
        return [];
    }
    public async Task<bool> QuitarPrerrequisito(string conexion, int asignatura, int requisito)
        => await DatabaseConnection.Delete(conexion, "aca_asignatura_prerrequisitos", new { cod_asignatura = asignatura, cod_asignatura_requisito = requisito });
    private static object Param(AsignaturaRequest r) => new { codigo = r.codigo.Trim().ToUpperInvariant(), nombre = r.nombre.Trim(), descripcion = Limpio(r.descripcion), r.creditos, r.horas_teoricas, r.horas_practicas, r.cod_tipo_asignatura, r.cod_estado };
    private static string? Limpio(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static long? ConsecutivoDeCodigo(string codigo, string prefijo)
    {
        var normalizado = codigo.Trim();
        if (!normalizado.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase)) return null;
        var sufijo = normalizado[prefijo.Length..];
        return long.TryParse(sufijo, NumberStyles.None, CultureInfo.InvariantCulture, out var consecutivo) && consecutivo > 0
            ? consecutivo
            : null;
    }

    private static string ConvertirARomano(int numero)
    {
        var valores = new (int Valor, string Simbolo)[]
        {
            (50, "L"), (40, "XL"), (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I"),
        };
        var resultado = new System.Text.StringBuilder();
        foreach (var (valor, simbolo) in valores)
        {
            while (numero >= valor)
            {
                resultado.Append(simbolo);
                numero -= valor;
            }
        }
        return resultado.ToString();
    }

    private sealed class PrerequisiteEdge { public int cod_asignatura { get; set; } public int cod_asignatura_requisito { get; set; } }
    private sealed class DependenciaAsignatura { public string relacion { get; set; } = string.Empty; public long cantidad { get; set; } }
}
