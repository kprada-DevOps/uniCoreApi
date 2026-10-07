namespace UniCore.Api.Modules.Seguridad.Dto;

/// <summary>Proyección de lectura de la relación <c>seg_rol_permisos</c>.</summary>
public sealed class RolPermisoDto
{
    public long cod { get; set; }
    public int cod_rol { get; set; }
    public int cod_permiso { get; set; }
    public DateTime createdday { get; set; }
}
