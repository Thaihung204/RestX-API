using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RestX.BLL.DataTranferObjects.Common;
using RestX.BLL.Interfaces.Auth;
using RestX.Models.Admin;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace RestX.BLL.Services.Auth
{
    public class AdminTokenService : IAdminTokenService
    {
        private readonly JwtSettings jwtSettings;

        public AdminTokenService(IOptions<JwtSettings> jwtSettings)
        {
            this.jwtSettings = jwtSettings.Value;
        }

        public string GenerateAccessToken(Admin admin, IList<string> roles)
        {
            var claims = BuildClaims(admin, roles);
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
            => DateTime.UtcNow.AddHours(7).AddMinutes(jwtSettings.AccessTokenExpiryMinutes);

        public DateTime GetRefreshTokenExpiry()
            => DateTime.UtcNow.AddHours(7).AddDays(jwtSettings.RefreshTokenExpiryDays);

        private static List<Claim> BuildClaims(Admin admin, IList<string> roles)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, admin.Id),
                new(ClaimTypes.Email, admin.Email ?? string.Empty),
                new(ClaimTypes.Name, admin.UserName ?? string.Empty),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
            return claims;
        }
    }
}
