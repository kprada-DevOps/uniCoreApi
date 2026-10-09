namespace UniCore.Api.Modules.PlanEstudios.Managers;

public sealed class AsignaturasDuplicadasException(IReadOnlyCollection<string> codigos)
    : InvalidOperationException($"Los siguientes códigos ya están registrados: {string.Join(", ", codigos)}.");
