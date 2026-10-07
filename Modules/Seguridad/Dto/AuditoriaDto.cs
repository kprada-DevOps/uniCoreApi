namespace UniCore.Api.Modules.Seguridad.Dto;

/// <summary>
/// Proyección de lectura de <c>seg_auditoria</c>. Los JSON se representan como texto
/// para conservar el contenido almacenado sin introducir tipos de dominio prematuros.
/// </summary>
public sealed class AuditoriaDto
{
    public long cod { get; set; }
    public long? cod_usuario { get; set; }
    public string tabla { get; set; } = string.Empty;
    public string? cod_registro { get; set; }
    public string accion { get; set; } = string.Empty;
    public string? datos_anteriores { get; set; }
    public string? datos_nuevos { get; set; }
    public DateTime fecha { get; set; }
    public string? ip { get; set; }
    public string? user_agent { get; set; }
}
