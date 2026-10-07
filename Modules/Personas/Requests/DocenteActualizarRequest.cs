namespace UniCore.Api.Modules.Personas.Requests;

/// <summary>
/// Datos para actualizar un docente. Hereda de <see cref="DocenteRequest"/> para que
/// ambos conjuntos de campos no diverjan.
/// </summary>
/// <remarks>
/// Semantica de PUT completa. Poner estado en INACTIVO es equivalente a la baja
/// logica, y volver a ACTIVO la revierte.
/// </remarks>
public sealed class DocenteActualizarRequest : DocenteRequest
{
}
