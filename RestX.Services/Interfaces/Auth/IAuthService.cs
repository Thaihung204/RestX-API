using RestX.BLL.DTOs.Auth;

namespace RestX.BLL.Interfaces.Auth
{
    public interface IAuthService
    {
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> LogoutAsync(Guid userId);
        Task<AuthResponse> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
        Task<AuthResponse> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request);
        Task<AuthResponse> RegisterCustomerAsync(RegisterCustomerRequest request);
        Task<AuthResponse> RefreshTokenAsync(string refreshToken);
    }
}
