using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using UniCore.Api.Configuration;
using UniCore.Api.Database;
using UniCore.Api.Helpers;
using UniCore.Api.Modules.Auth.Dto;
using UniCore.Api.Modules.Auth.Managers;
using UniCore.Api.Modules.Auth.Requests;

namespace UniCore.Api.Modules.Auth.Controllers;

/// <summary>
/// AutenticaciÃ³n del sistema, con el mismo contrato que el proyecto de referencia:
/// la conexiÃ³n llega por la ruta, el login es la Ãºnica acciÃ³n anÃ³nima y se emiten
/// dos tokens (acceso de 1 dÃ­a y refresco de 15) con el claim <c>type</c>.
/// </summary>
[ApiController]
[Route("{conexion}/[controller]")]
[Authorize]
public sealed class AuthController : ControllerBase
{
    /// <summary>
    /// Mensaje Ãºnico para usuario inexistente, inactivo y contraseÃ±a incorrecta.
    /// Distinguirlos revelarÃ­a quÃ© usernames existen en el sistema.
    /// </summary>
    private const string MensajeCredencialesInvalidas = "Credenciales incorrectas";

    private const int DuracionTokenDias = 1;
    private const int DuracionRefreshDias = 15;

    private const string TipoToken = "token";
    private const string TipoRefresh = "refresh";

    private readonly AuthManager _authManager;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AuthManager authManager, Microsoft.Extensions.Options.IOptions<JwtOptions> jwtOptions, ILogger<AuthController> logger)
    {
        _authManager = authManager;
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Autentica un usuario y devuelve su token de acceso y su token de refresco.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("Login")]
    public async Task<ActionResult> Login(string conexion, [FromBody] LoginUsuarioRequest request)
    {
        UsuarioDto? usuario;

        try
        {
            usuario = await _authManager.ObtenerUsuarioPorUsername(request.user, conexion);

            if (usuario is null)
            {
                _logger.LogWarning("Intento de login con usuario inexistente: {Username}", request.user);
                return Respuesta.UnAuthorized(MensajeCredencialesInvalidas);
            }

            if (!usuario.activo)
            {
                _logger.LogWarning("Intento de login de usuario inactivo: {Username}", request.user);
                return Respuesta.UnAuthorized(MensajeCredencialesInvalidas);
            }

            if (!BCrypt.Net.BCrypt.Verify(request.password, usuario.password_hash))
            {
                _logger.LogWarning("ContraseÃ±a incorrecta para el usuario: {Username}", request.user);
                return Respuesta.UnAuthorized(MensajeCredencialesInvalidas);
            }

            usuario = await _authManager.CompletarPermisos(usuario, conexion);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // El hash almacenado no es BCrypt vÃ¡lido: es un problema de datos, no del cliente.
            _logger.LogError("El hash almacenado para el usuario {Username} no es un BCrypt vÃ¡lido.", request.user);
            return Respuesta.Failed<string>(mensaje: "Error de configuraciÃ³n de credenciales");
        }

        await _authManager.ActualizarUltimoAcceso(usuario.cod, conexion);

        var dto = new LoginDto
        {
            usuario = usuario,
            token = GenerarToken(conexion, usuario, TipoToken, DuracionTokenDias),
            refresh = GenerarToken(conexion, usuario, TipoRefresh, DuracionRefreshDias),
        };

        _logger.LogInformation("Login correcto del usuario {Username} en {Conexion} con {Roles} rol(es)",
            usuario.username, conexion, usuario.roles.Count);

        return Respuesta.Success(dto, "Inicio de sesiÃ³n correcto");
    }

    /// <summary>
    /// Reemite token y refresh a partir de un refresh token vÃ¡lido.
    /// Sin estado: solo comprueba los claims, igual que la referencia.
    /// </summary>
    [HttpGet("refresh")]
    public async Task<ActionResult> Refresh(string conexion)
    {
        var tipo = User.FindFirst("type")?.Value;
        var colegio = User.FindFirst("colegio")?.Value;

        if (!string.Equals(tipo, TipoRefresh, StringComparison.Ordinal))
            return Respuesta.UnAuthorized("El token enviado no es de tipo refresh");

        if (!string.Equals(colegio, conexion, StringComparison.OrdinalIgnoreCase))
            return Respuesta.UnAuthorized("La conexiÃ³n del token no coincide con la de la peticiÃ³n");

        var codUsuario = User.FindFirst("codusuario")?.Value;
        if (!long.TryParse(codUsuario, out var cod) || cod <= 0)
            return Respuesta.UnAuthorized("Token de refresh invalido");

        var usuario = await _authManager.ObtenerUsuarioPorCod(cod, conexion);
        if (usuario is null)
            return Respuesta.UnAuthorized("Usuario no encontrado");

        if (!usuario.activo)
            return Respuesta.UnAuthorized("Usuario Suspendido");

        usuario = await _authManager.CompletarPermisos(usuario, conexion);

        return Respuesta.Success(new LoginDto
        {
            usuario = usuario,
            token = GenerarToken(conexion, usuario, TipoToken, DuracionTokenDias),
            refresh = GenerarToken(conexion, usuario, TipoRefresh, DuracionRefreshDias),
        }, "Token renovado");
    }

    /// <summary>
    /// Datos del usuario autenticado, leÃ­dos de sus claims y completados desde la base.
    /// </summary>
    [HttpGet("ObtenerUsuario")]
    public async Task<ActionResult> ObtenerUsuario(string conexion)
    {
        var codUsuario = User.FindFirst("codusuario")?.Value;
        if (!long.TryParse(codUsuario, out var cod) || cod <= 0)
            return Respuesta.UnAuthorized();

        var usuario = await _authManager.ObtenerUsuarioPorCod(cod, conexion);
        if (usuario is null)
            return Respuesta.UnAuthorized("Usuario no encontrado");

        usuario = await _authManager.CompletarPermisos(usuario, conexion);
        return Respuesta.Success(usuario);
    }

    /// <summary>
    /// Roles y permisos efectivos del usuario autenticado, para construir el menÃº.
    /// </summary>
    [HttpGet("ObtenerPermisos")]
    public async Task<ActionResult> ObtenerPermisos(string conexion)
    {
        var codUsuario = User.FindFirst("codusuario")?.Value;
        if (!long.TryParse(codUsuario, out var cod) || cod <= 0)
            return Respuesta.UnAuthorized();

        var usuario = await _authManager.ObtenerUsuarioPorCod(cod, conexion);
        if (usuario is null)
            return Respuesta.UnAuthorized("Usuario no encontrado");

        usuario = await _authManager.CompletarPermisos(usuario, conexion);

        return Respuesta.Success(new { roles = usuario.roles, permisos = usuario.permisos });
    }

    /// <summary>
    /// Token con los mismos claims que el proyecto de referencia.
    /// Se omiten codtipousuario, codperfil, identificacion y moodleid_usuario:
    /// el esquema de UniCore no tiene esas columnas.
    /// </summary>
    private string GenerarToken(string conexion, UsuarioDto usuario, string tipo, int duracionDias)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.TokenSecret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        List<Claim> claims =
        [
            new Claim("codusuario", usuario.cod.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("colegio", conexion),
            new Claim("type", tipo),
            // Un rol por claim, para que [Authorize(Roles="...")] funcione con varios.
            .. usuario.roles.Select(rol => new Claim(ClaimTypes.Role, rol)),
            new Claim("permisos", string.Join(",", usuario.permisos)),
            new Claim("fechahora_token", DbHelpers.GetFechaActual(conexion)),
        ];

        var expiracion = DbHelpers.GetFechaActualDatetime(conexion).AddDays(duracionDias);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: string.IsNullOrWhiteSpace(_jwtOptions.Audience) ? _jwtOptions.Issuer : _jwtOptions.Audience,
            claims: claims,
            expires: expiracion,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
