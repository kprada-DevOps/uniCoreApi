using System.ComponentModel.DataAnnotations;

namespace UniCore.Api.Modules.Personas.Requests;

/// <summary>
/// Datos para dar de alta una persona en per_personas.
/// Los nombres de las propiedades coinciden con las columnas porque el manager
/// las proyecta sobre la sentencia de INSERT.
/// </summary>
public class PersonaRequest
{
    /// <summary>
    /// [Required] no sirve en un int no nulable: siempre se cumple porque el valor
    /// por defecto (0) ya cuenta como "presente". Por eso se usa [Range] para
    /// rechazar el 0 de los tipos de documento inexistentes.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "El tipo de documento es obligatorio.")]
    public int cod_tipo_documento { get; set; }

    [Required(ErrorMessage = "El número de documento es obligatorio.")]
    [StringLength(30, ErrorMessage = "El número de documento no puede superar los 30 caracteres.")]
    public string numero_documento { get; set; } = string.Empty;

    [Required(ErrorMessage = "El primer nombre es obligatorio.")]
    [StringLength(80, ErrorMessage = "El primer nombre no puede superar los 80 caracteres.")]
    public string primer_nombre { get; set; } = string.Empty;

    [StringLength(80, ErrorMessage = "El segundo nombre no puede superar los 80 caracteres.")]
    public string? segundo_nombre { get; set; }

    [Required(ErrorMessage = "El primer apellido es obligatorio.")]
    [StringLength(80, ErrorMessage = "El primer apellido no puede superar los 80 caracteres.")]
    public string primer_apellido { get; set; } = string.Empty;

    [StringLength(80, ErrorMessage = "El segundo apellido no puede superar los 80 caracteres.")]
    public string? segundo_apellido { get; set; }

    public DateTime? fecha_nacimiento { get; set; }

    [StringLength(30, ErrorMessage = "El sexo no puede superar los 30 caracteres.")]
    public string? sexo { get; set; }

    [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
    [StringLength(180, ErrorMessage = "El correo electrónico no puede superar los 180 caracteres.")]
    public string? email { get; set; }

    [StringLength(40, ErrorMessage = "El teléfono no puede superar los 40 caracteres.")]
    public string? telefono { get; set; }

    [StringLength(40, ErrorMessage = "El celular no puede superar los 40 caracteres.")]
    public string? celular { get; set; }

    [StringLength(250, ErrorMessage = "La dirección no puede superar los 250 caracteres.")]
    public string? direccion { get; set; }

    [StringLength(100, ErrorMessage = "La ciudad no puede superar los 100 caracteres.")]
    public string? ciudad { get; set; }

    [StringLength(100, ErrorMessage = "El departamento no puede superar los 100 caracteres.")]
    public string? departamento { get; set; }

    [StringLength(100, ErrorMessage = "El país no puede superar los 100 caracteres.")]
    public string? pais { get; set; }

    /// <summary>
    /// Permite dar de alta a una persona ya inactiva. En un alta normal se deja en
    /// true, que es lo que quisera MySQL por defecto.
    /// </summary>
    public bool activo { get; set; } = true;
}
