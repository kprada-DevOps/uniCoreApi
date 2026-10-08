using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace UniCore.Api.Modules.Seguridad.Authorization;

public sealed class PermisoPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    public const string Prefix = "PERMISO:";
    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var esPermiso = policyName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);
        if (!esPermiso) return base.GetPolicyAsync(policyName);
        var codigo = policyName[Prefix.Length..].Trim().ToUpperInvariant();
        if (codigo.Length == 0) return Task.FromResult<AuthorizationPolicy?>(null);
        var builder = new AuthorizationPolicyBuilder().RequireAuthenticatedUser();
        builder.AddRequirements(new PermisoRequirement(codigo));
        var policy = builder.Build();
        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
