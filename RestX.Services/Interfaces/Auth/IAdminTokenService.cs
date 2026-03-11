using RestX.Models.Admin;

namespace RestX.BLL.Interfaces.Auth
{
    public interface IAdminTokenService
    {
        string GenerateAccessToken(Admin admin, IList<string> roles);
        string GenerateRefreshToken();
        DateTime GetAccessTokenExpiry();
        DateTime GetRefreshTokenExpiry();
    }
}
