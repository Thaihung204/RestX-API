using RestX.BLL.DataTranferObjects.Auth;
namespace RestX.BLL.Interfaces.Auth
{
    public interface IAuthService
    {
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> LogoutAsync(Guid userId);
        Task<AuthResponse> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
        Task<AuthResponse> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request);
        Task<AuthResponse> RefreshTokenAsync(string refreshToken);
        Task<CheckPhoneResponse> CheckPhoneNumberAsync(string phoneNumber);
        Task<AuthResponse> CustomerPhoneLoginAsync(CustomerPhoneLoginRequest request);
        Task<AuthResponse> CustomerPhoneRegisterAsync(CustomerPhoneRegisterRequest request);
    }
}
