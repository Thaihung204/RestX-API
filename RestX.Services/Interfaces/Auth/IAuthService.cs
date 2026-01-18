using RestX.BLL.DTOs.Auth;

namespace RestX.BLL.Interfaces.Auth
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
        Task<AuthResponseDto> LogoutAsync(Guid userId);
        Task<AuthResponseDto> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request);
        Task<AuthResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto request);
        Task<AuthResponseDto> ResetPasswordAsync(ResetPasswordRequestDto request);
        Task<AuthResponseDto> RegisterCustomerAsync(RegisterCustomerRequestDto request);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
    }
}
