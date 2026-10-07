namespace UniCore.Api.Modules.Seguridad.Dto;

/// <summary>Proyección de lectura de la relación <c>seg_usuario_roles</c>.</summary>
public sealed class UsuarioRolDto
{
    public long cod { get; set; }
    public long cod_usuario { get; set; }
    public int cod_rol { get; set; }
    public DateTime createdday { get; set; }
}
