namespace UniCore.Api.Modules.Auth.Dto;

/// <summary>
/// Permiso heredado por un usuario a través de sus roles.
/// El código tiene el formato "MODULO.ACCION" (ej. PERSONAS.CONSULTAR).
/// </summary>
public sealed class PermisoDto
{
    public int Cod { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Modulo { get; set; } = string.Empty;

    public string Accion { get; set; } = string.Empty;

    public bool Activo { get; set; }
}
