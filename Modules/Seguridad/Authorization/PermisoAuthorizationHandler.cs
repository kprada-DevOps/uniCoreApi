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
        if (await auth.EsSuperadmin(usuario, conexion))
        {
            context.Succeed(requirement);
            return;
        }
        var permisos = await auth.ObtenerPermisosUsuario(usuario, conexion);
        if (permisos.Contains(requirement.Codigo, StringComparer.OrdinalIgnoreCase)) context.Succeed(requirement);
    }
}
