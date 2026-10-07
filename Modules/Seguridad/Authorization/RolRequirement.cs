using Microsoft.AspNetCore.Authorization;
namespace UniCore.Api.Modules.Seguridad.Authorization;
public sealed record RolRequirement(string Codigo) : IAuthorizationRequirement;
