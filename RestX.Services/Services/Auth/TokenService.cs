using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RestX.BLL.DataTranferObjects.Common;
using RestX.BLL.Interfaces.Auth;
using RestX.Models.Identity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace RestX.BLL.Services.Auth
{
    public class TokenService : ITokenService
    {
        private readonly JwtSettings jwtSettings;

        public TokenService(IOptions<JwtSettings> jwtSettings)
        {
            this.jwtSettings = jwtSettings.Value;
        }

        public string GenerateAccessToken(ApplicationUser user, IList<string> roles, string hostname)
        {
            var claims = BuildClaims(user, roles, hostname);
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSettings.Secret));
             var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtSettings.Issuer,
                audience: jwtSettings.Audience,
                claims: claims,
                expires: GetAccessTokenExpiry(),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public DateTime GetAccessTokenExpiry()
            => DateTime.UtcNow.AddMinutes(jwtSettings.AccessTokenExpiryMinutes);

        public DateTime GetRefreshTokenExpiry()
            => DateTime.UtcNow.AddDays(jwtSettings.RefreshTokenExpiryDays);

        private static List<Claim> BuildClaims(ApplicationUser user, IList<string> roles, string hostname)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(ClaimTypes.Name, user.UserName ?? string.Empty),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new("tenant_hostname", hostname ?? string.Empty)
            };

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
            return claims;
        }
    }
}
