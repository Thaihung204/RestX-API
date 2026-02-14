using RestX.Models.Identity;

namespace RestX.BLL.Interfaces.Auth
{
    public interface ITokenService
    {
        string GenerateAccessToken(ApplicationUser user, IList<string> roles, string hostname);
        string GenerateRefreshToken();
        DateTime GetAccessTokenExpiry();
        DateTime GetRefreshTokenExpiry();
    }
}
