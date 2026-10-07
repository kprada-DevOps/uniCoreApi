namespace UniCore.Api.Modules.Personas.Requests;

/// <summary>
/// Datos para actualizar una persona. Hereda de <see cref="PersonaRequest"/> para que
/// los dos conjuntos de campos no diverjan al anadir una columna.
/// </summary>
/// <remarks>
/// Semantica de PUT completa: la persona se reemplaza por estos valores, de modo que
/// un campo opcional que llegue en null se limpia en la base de datos. El codigo no
/// viaja en el cuerpo, va en la ruta.
/// <para>
/// La actualizacion tambien puede reactivar la persona poniendo activo en true, que
/// es la forma de revertir una baja logica.
/// </para>
/// </remarks>
public sealed class PersonaActualizarRequest : PersonaRequest
{
}
