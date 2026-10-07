using Microsoft.AspNetCore.Authorization;
using UniCore.Api.Modules.Auth.Managers;
using UniCore.Api.Middleware;

namespace UniCore.Api.Modules.Seguridad.Authorization;

public sealed class PermisoAuthorizationHandler(AuthManager auth, IHttpContextAccessor accessor) : AuthorizationHandler<PermisoRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermisoRequirement requirement)
    {
        var http = accessor.HttpContext;
        var conexion = http is null ? null : CadenaConnectMiddleware.GetConnection(http);
        if (string.IsNullOrWhiteSpace(conexion) || !long.TryParse(context.User.FindFirst("codusuario")?.Value, out var usuario)) return;
        var permisos = await auth.ObtenerPermisosUsuario(usuario, conexion);
        if (permisos.Contains(requirement.Codigo, StringComparer.OrdinalIgnoreCase)) context.Succeed(requirement);
    }
}

public sealed class RolAuthorizationHandler(AuthManager auth, IHttpContextAccessor accessor) : AuthorizationHandler<RolRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, RolRequirement requirement)
    {
        var http = accessor.HttpContext;
        var conexion = http is null ? null : CadenaConnectMiddleware.GetConnection(http);
        if (string.IsNullOrWhiteSpace(conexion) || !long.TryParse(context.User.FindFirst("codusuario")?.Value, out var usuario)) return;
        var roles = await auth.ObtenerRolesUsuario(usuario, conexion);
        if (roles.Contains(requirement.Codigo, StringComparer.OrdinalIgnoreCase)) context.Succeed(requirement);
    }
}
