using Microsoft.AspNetCore.Authorization;

namespace UniCore.Api.Modules.Seguridad.Authorization;

public sealed record PermisoRequirement(string Codigo) : IAuthorizationRequirement;
