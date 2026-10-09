using UniCore.Api.Database;
using UniCore.Api.Modules.PlanEstudios.Dto;
using UniCore.Api.Modules.PlanEstudios.Requests;

namespace UniCore.Api.Modules.PlanEstudios.Managers;

public sealed class PlanesEstudioManager
{
    private const string Select = @"SELECT p.cod,p.cod_programa,pr.nombre AS programa,p.codigo,p.nombre,p.version,p.fecha_inicio,p.fecha_fin,p.cod_estado,ep.nombre AS estado,p.createdday,p.updatedday FROM aca_planes_estudio p JOIN aca_programas pr ON pr.cod=p.cod_programa JOIN aca_estados_plan_estudio ep ON ep.cod=p.cod_estado";
    public Task<List<PlanEstudioDto>> Listar(string conexion, int? programa, bool incluirInactivos)
        => DatabaseConnection.GetMany<PlanEstudioDto>(conexion, $"{Select} WHERE (@programa IS NULL OR p.cod_programa=@programa) AND (@inactivos=1 OR ep.codigo<>'INACTIVO') ORDER BY pr.nombre,p.fecha_inicio DESC,p.codigo;", new { programa, inactivos = incluirInactivos });
    public async Task<PlanEstudioDto?> Obtener(string conexion, int cod)
    {
        var plan = await DatabaseConnection.GetOne<PlanEstudioDto>(conexion, $"{Select} WHERE p.cod=@cod LIMIT 1;", new { cod });
        if (plan is null) return null;
        var elementos = await DatabaseConnection.GetMany<PlanElementoDto>(conexion, @"SELECT pe.cod,pe.cod_plan_estudio,pe.cod_asignatura,a.codigo AS asignatura_codigo,a.nombre AS asignatura_nombre,pe.cod_componente,c.codigo AS componente_codigo,c.nombre AS componente_nombre,pe.semestre,pe.creditos,pe.orden,pe.cod_estado,ec.nombre AS estado FROM aca_plan_elementos pe LEFT JOIN aca_asignaturas a ON a.cod=pe.cod_asignatura LEFT JOIN aca_componentes c ON c.cod=pe.cod_componente JOIN aca_estados_configuracion ec ON ec.cod=pe.cod_estado WHERE pe.cod_plan_estudio=@cod ORDER BY pe.semestre,pe.orden,pe.cod;", new { cod });
        plan.elementos = elementos;
        var componentIds = elementos.Where(e => e.cod_componente.HasValue).Select(e => e.cod_componente!.Value).Distinct().ToArray();
        if (componentIds.Length > 0)
        {
            var opciones = await DatabaseConnection.GetMany<ComponenteOpcionDto>(conexion, @"SELECT o.cod,o.cod_componente,o.cod_asignatura,a.codigo AS codigo_asignatura,a.nombre AS asignatura,o.orden,o.cod_estado,ec.nombre AS estado FROM aca_componente_opciones o JOIN aca_asignaturas a ON a.cod=o.cod_asignatura JOIN aca_estados_configuracion ec ON ec.cod=o.cod_estado WHERE o.cod_componente IN @codigos ORDER BY o.cod_componente,o.orden,a.nombre;", new { codigos = componentIds });
            foreach (var elemento in elementos.Where(e => e.cod_componente.HasValue))
                elemento.opciones = opciones.Where(o => o.cod_componente == elemento.cod_componente).ToList();
        }
        plan.requisitos = await DatabaseConnection.GetMany<PlanRequisitoDto>(conexion, @"SELECT pr.cod,pr.cod_plan_estudio,pr.cod_requisito,r.codigo AS codigo_requisito,r.nombre AS requisito,pr.obligatorio,pr.orden,pr.cod_estado,e.nombre AS estado FROM aca_plan_requisitos pr JOIN aca_requisitos_cocurriculares r ON r.cod=pr.cod_requisito JOIN aca_estados_configuracion e ON e.cod=pr.cod_estado WHERE pr.cod_plan_estudio=@cod ORDER BY pr.orden,r.nombre;", new { cod });
        return plan;
    }
    public Task<long?> ExisteCodigo(string conexion, int programa, string codigo, int? omitir)
        => DatabaseConnection.ExecuteScalar<long?>(conexion, "SELECT cod FROM aca_planes_estudio WHERE cod_programa=@programa AND codigo=@codigo AND (@omitir IS NULL OR cod<>@omitir) LIMIT 1;", new { programa, codigo, omitir });
    public Task<bool> ProgramaExiste(string conexion, int cod) => DatabaseConnection.ExecuteScalar<bool>(conexion, "SELECT EXISTS(SELECT 1 FROM aca_programas WHERE cod=@cod);", new { cod });
    public Task<bool> EstadoExiste(string conexion, int cod) => DatabaseConnection.ExecuteScalar<bool>(conexion, "SELECT EXISTS(SELECT 1 FROM aca_estados_plan_estudio WHERE cod=@cod AND activo=1);", new { cod });
    public async Task<int> Crear(string conexion, PlanEstudioRequest r)
    {
        var id = await DatabaseConnection.Insert(conexion, "aca_planes_estudio", Param(r));
        return checked((int)id);
    }
    public async Task<bool> Actualizar(string conexion, int cod, PlanEstudioRequest r)
        => await DatabaseConnection.Execute(conexion, "UPDATE aca_planes_estudio SET cod_programa=@cod_programa,codigo=@codigo,nombre=@nombre,version=@version,fecha_inicio=@fecha_inicio,fecha_fin=@fecha_fin,cod_estado=@cod_estado,updatedday=NOW() WHERE cod=@cod;", new { cod, r.cod_programa, codigo = r.codigo.Trim().ToUpperInvariant(), nombre = r.nombre.Trim(), version = r.version.Trim(), r.fecha_inicio, r.fecha_fin, r.cod_estado }) > 0;
    public async Task<bool> CambiarEstado(string conexion, int cod, int estado)
        => await DatabaseConnection.Execute(conexion, "UPDATE aca_planes_estudio SET cod_estado=@estado,updatedday=NOW() WHERE cod=@cod;", new { cod, estado }) > 0 || await Obtener(conexion, cod) is not null;

    public async Task ReemplazarElementos(string conexion, int plan, IReadOnlyCollection<PlanElementoRequest> items)
    {
        if (items.Any(i => i.cod_asignatura.HasValue == i.cod_componente.HasValue))
            throw new InvalidOperationException("Cada elemento del plan debe referenciar una asignatura o un componente, exactamente uno.");
        if (items.Any(i => i.semestre < 1 || i.creditos < 0)) throw new InvalidOperationException("Semestre o créditos no válidos.");
        var assignments = items.Where(i => i.cod_asignatura.HasValue).Select(i => i.cod_asignatura!.Value).ToArray();
        var components = items.Where(i => i.cod_componente.HasValue).Select(i => i.cod_componente!.Value).ToArray();
        if (assignments.Distinct().Count() != assignments.Length || components.Distinct().Count() != components.Length)
            throw new InvalidOperationException("El plan contiene elementos duplicados.");
        await using var tx = DatabaseConnection.BeginTransaction(conexion);
        if (assignments.Length > 0 && (await DatabaseConnection.GetManyTransaccion<int>(tx, "SELECT cod FROM aca_asignaturas WHERE cod IN @ids;", new { ids = assignments })).Count != assignments.Distinct().Count())
            throw new InvalidOperationException("Una asignatura indicada no existe.");
        if (components.Length > 0 && (await DatabaseConnection.GetManyTransaccion<int>(tx, "SELECT cod FROM aca_componentes WHERE cod IN @ids;", new { ids = components })).Count != components.Distinct().Count())
            throw new InvalidOperationException("Un componente indicado no existe.");
        if (items.Count > 0 && (await DatabaseConnection.GetManyTransaccion<int>(tx, "SELECT cod FROM aca_estados_configuracion WHERE cod IN @ids AND activo=1;", new { ids = items.Select(i => i.cod_estado).Distinct().ToArray() })).Count != items.Select(i => i.cod_estado).Distinct().Count())
            throw new InvalidOperationException("Un estado de elemento no existe o está inactivo.");
        await DatabaseConnection.ExecuteTransaccion(tx, "DELETE FROM aca_plan_elementos WHERE cod_plan_estudio=@plan;", new { plan });
        foreach (var item in items)
            await DatabaseConnection.ExecuteTransaccion(tx, "INSERT INTO aca_plan_elementos(cod_plan_estudio,cod_asignatura,cod_componente,semestre,creditos,orden,cod_estado) VALUES(@plan,@cod_asignatura,@cod_componente,@semestre,@creditos,@orden,@cod_estado);", new { plan, item.cod_asignatura, item.cod_componente, item.semestre, item.creditos, item.orden, item.cod_estado });
        tx.Commit();
    }

    public async Task ReemplazarRequisitos(string conexion, int plan, IReadOnlyCollection<PlanRequisitoRequest> items)
    {
        var ids = items.Select(i => i.cod_requisito).ToArray();
        if (ids.Distinct().Count() != ids.Length) throw new InvalidOperationException("Hay requisitos repetidos.");
        await using var tx = DatabaseConnection.BeginTransaction(conexion);
        if (ids.Length > 0 && (await DatabaseConnection.GetManyTransaccion<int>(tx, "SELECT cod FROM aca_requisitos_cocurriculares WHERE cod IN @ids;", new { ids })).Count != ids.Distinct().Count())
            throw new InvalidOperationException("Un requisito cocurricular indicado no existe.");
        var estados = items.Select(i => i.cod_estado).Distinct().ToArray();
        if (estados.Length > 0 && (await DatabaseConnection.GetManyTransaccion<int>(tx, "SELECT cod FROM aca_estados_configuracion WHERE cod IN @ids AND activo=1;", new { ids = estados })).Count != estados.Length)
            throw new InvalidOperationException("Un estado de requisito del plan no existe o está inactivo.");
        var existentes = await DatabaseConnection.GetManyTransaccion<PlanRequisitoPersistido>(tx, "SELECT cod,cod_requisito FROM aca_plan_requisitos WHERE cod_plan_estudio=@plan;", new { plan });
        foreach (var item in items)
        {
            var existente = existentes.FirstOrDefault(x => x.cod_requisito == item.cod_requisito);
            if (existente is null)
                await DatabaseConnection.ExecuteTransaccion(tx, "INSERT INTO aca_plan_requisitos(cod_plan_estudio,cod_requisito,obligatorio,orden,cod_estado) VALUES(@plan,@cod_requisito,@obligatorio,@orden,@cod_estado);", new { plan, item.cod_requisito, item.obligatorio, item.orden, item.cod_estado });
            else
                await DatabaseConnection.ExecuteTransaccion(tx, "UPDATE aca_plan_requisitos SET obligatorio=@obligatorio,orden=@orden,cod_estado=@cod_estado WHERE cod=@cod;", new { cod = existente.cod, item.obligatorio, item.orden, item.cod_estado });
        }
        var inactivo = await DatabaseConnection.ExecuteScalarTransaccion<int?>(tx, "SELECT cod FROM aca_estados_configuracion WHERE codigo='INACTIVO' AND activo=1 LIMIT 1;");
        foreach (var omitido in existentes.Where(e => !ids.Contains(e.cod_requisito)))
        {
            var tieneSeguimiento = await DatabaseConnection.ExecuteScalarTransaccion<bool>(tx, "SELECT EXISTS(SELECT 1 FROM aca_estudiante_requisitos WHERE cod_plan_requisito=@cod);", new { cod = omitido.cod });
            if (tieneSeguimiento)
            {
                if (!inactivo.HasValue) throw new InvalidOperationException("No se puede retirar un requisito con seguimiento estudiantil porque falta el estado INACTIVO.");
                await DatabaseConnection.ExecuteTransaccion(tx, "UPDATE aca_plan_requisitos SET cod_estado=@estado WHERE cod=@cod;", new { estado = inactivo.Value, cod = omitido.cod });
            }
            else await DatabaseConnection.ExecuteTransaccion(tx, "DELETE FROM aca_plan_requisitos WHERE cod=@cod;", new { cod = omitido.cod });
        }
        tx.Commit();
    }

    private static object Param(PlanEstudioRequest r) => new { r.cod_programa, codigo = r.codigo.Trim().ToUpperInvariant(), nombre = r.nombre.Trim(), version = r.version.Trim(), r.fecha_inicio, r.fecha_fin, r.cod_estado };
    private sealed class PlanRequisitoPersistido { public long cod { get; set; } public int cod_requisito { get; set; } }
}
