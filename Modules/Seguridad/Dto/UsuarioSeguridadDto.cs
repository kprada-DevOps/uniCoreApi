namespace UniCore.Api.Modules.Seguridad.Dto;

/// <summary>
/// Proyección administrativa de <c>seg_usuarios</c>. Excluye intencionalmente
/// <c>password_hash</c>; las credenciales solo se manejan en los flujos de autenticación.
/// </summary>
public sealed class UsuarioSeguridadDto
{
    public long cod { get; set; }
    public long? cod_persona { get; set; }
    public string username { get; set; } = string.Empty;
    public bool activo { get; set; }
    public DateTime? ultimo_acceso { get; set; }
    public DateTime createdday { get; set; }
    public DateTime? updatedday { get; set; }
    public string? persona_numero_documento { get; set; }
    public string? persona_nombre_completo { get; set; }
}
