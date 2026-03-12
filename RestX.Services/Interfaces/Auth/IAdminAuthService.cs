using RestX.BLL.DataTranferObjects.Authentication;

namespace RestX.BLL.Interfaces.Auth
{
    public interface IAdminAuthService
    {
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> LogoutAsync(string adminId);
        Task<AuthResponse> ChangePasswordAsync(string adminId, ChangePasswordRequest request);
        Task<AuthResponse> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request);
        Task<AuthResponse> RefreshTokenAsync(string refreshToken);
    }
}
