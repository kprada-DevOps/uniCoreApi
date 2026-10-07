using System.Security.Claims;
using System.Text.Json;
using UniCore.Api.Database;
using UniCore.Api.Modules.Seguridad.Dto;

namespace UniCore.Api.Modules.Seguridad.Managers;

/// <summary>Servicio central para escritura y consulta paginada de auditoría de seguridad.</summary>
public sealed class AuditoriaManager(DatabaseProvider db, IHttpContextAccessor accessor)
{
    private readonly DatabaseProvider _db = db;
    private readonly IHttpContextAccessor _accessor = accessor;

    public async Task Registrar(string conexion, string tabla, string? registro, string accion, object? anterior = null, object? nuevo = null)
    {
        var http = _accessor.HttpContext;
        var actor = long.TryParse(http?.User.FindFirst("codusuario")?.Value, out var id) ? id : (long?)null;
        var ip = http?.Connection.RemoteIpAddress?.ToString();
        var agent = http?.Request.Headers.UserAgent.ToString();
        if (agent?.Length > 500) agent = agent[..500];
        const string sql = @"INSERT INTO seg_auditoria(cod_usuario,tabla,cod_registro,accion,datos_anteriores,datos_nuevos,ip,user_agent)
VALUES(@actor,@tabla,@registro,@accion,@antes,@despues,@ip,@agent);";
        await _db.Execute(sql, new { actor, tabla, registro, accion, antes = anterior is null ? null : JsonSerializer.Serialize(anterior), despues = nuevo is null ? null : JsonSerializer.Serialize(nuevo), ip, agent }, conexion);
    }

    public async Task<ResultadoPaginadoDto<AuditoriaDto>> Listar(string conexion, int pagina, int tamano, long? usuario, string? tabla, DateTime? desde, DateTime? hasta)
    {
        pagina = Math.Max(1, pagina); tamano = Math.Clamp(tamano, 1, 100);
        const string where = "WHERE (@usuario IS NULL OR cod_usuario=@usuario) AND (@tabla IS NULL OR tabla=@tabla) AND (@desde IS NULL OR fecha>=@desde) AND (@hasta IS NULL OR fecha<DATE_ADD(@hasta, INTERVAL 1 DAY))";
        var args = new { usuario, tabla, desde, hasta, offset = ((long)pagina - 1) * tamano, tamano };
        var total = await _db.ExecuteScalar<long>($"SELECT COUNT(*) FROM seg_auditoria {where};", args, conexion);
        var items = await _db.GetMany<AuditoriaDto>($"SELECT cod,cod_usuario,tabla,cod_registro,accion,datos_anteriores,datos_nuevos,fecha,ip,user_agent FROM seg_auditoria {where} ORDER BY fecha DESC,cod DESC LIMIT @tamano OFFSET @offset;", args, conexion);
        return new ResultadoPaginadoDto<AuditoriaDto> { items = items, pagina = pagina, tamanoPagina = tamano, total = total };
    }
}
