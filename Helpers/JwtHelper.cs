using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using UniCore.Api.Configuration;

namespace UniCore.Api.Helpers;

/// <summary>
/// Emisión de tokens JWT.
/// Reutilizable por cualquier módulo que necesite autenticar.
/// </summary>
public sealed class JwtHelper
{
    public const string ClaimConexion = "conexion";
    public const string ClaimCodUsuario = "codusuario";
    public const string ClaimCodPersona = "codpersona";
    public const string ClaimRoles = "roles";
    public const string ClaimPermisos = "permisos";

    private readonly JwtOptions _options;

    public JwtHelper(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// Genera un token firmado con los datos de identidad, roles y permisos del usuario.
    /// </summary>
    public (string Token, DateTime ExpiraEn) Generate(
        long codUsuario,
        string username,
        long? codPersona,
        string conexion,
        IEnumerable<string> roles,
        IEnumerable<string> permisos)
    {
        var expiraEn = DateTime.UtcNow.AddHours(_options.ExpirationHours);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, codUsuario.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new(ClaimConexion, conexion),
            new(ClaimCodUsuario, codUsuario.ToString()),
        };

        if (codPersona.HasValue)
            claims.Add(new Claim(ClaimCodPersona, codPersona.Value.ToString()));

        // Un claim por rol y por permiso, para que AuthorizationPolicy los lea directamente.
        claims.AddRange(roles.Select(rol => new Claim(ClaimTypes.Role, rol)));
        claims.AddRange(permisos.Select(permiso => new Claim(ClaimPermisos, permiso)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.TokenSecret)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: string.IsNullOrWhiteSpace(_options.Audience) ? _options.Issuer : _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiraEn,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiraEn);
    }
}
