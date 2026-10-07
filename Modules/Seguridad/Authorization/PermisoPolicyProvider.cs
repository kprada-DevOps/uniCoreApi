using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace UniCore.Api.Modules.Seguridad.Authorization;

public sealed class PermisoPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    public const string Prefix = "PERMISO:";
    public const string RolePrefix = "ROL:";
    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var esPermiso = policyName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);
        var esRol = policyName.StartsWith(RolePrefix, StringComparison.OrdinalIgnoreCase);
        if (!esPermiso && !esRol) return base.GetPolicyAsync(policyName);
        var codigo = policyName[(esPermiso ? Prefix.Length : RolePrefix.Length)..].Trim().ToUpperInvariant();
        if (codigo.Length == 0) return Task.FromResult<AuthorizationPolicy?>(null);
        var builder = new AuthorizationPolicyBuilder().RequireAuthenticatedUser();
        if (esPermiso) builder.AddRequirements(new PermisoRequirement(codigo));
        if (esRol) builder.AddRequirements(new RolRequirement(codigo));
        var policy = builder.Build();
        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
