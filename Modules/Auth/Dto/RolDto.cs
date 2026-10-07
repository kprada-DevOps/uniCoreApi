namespace UniCore.Api.Modules.Auth.Dto;

/// <summary>
/// Rol asignado a un usuario. Mapeado desde seg_roles.
/// </summary>
public sealed class RolDto
{
    public int Cod { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public bool Activo { get; set; }
}
