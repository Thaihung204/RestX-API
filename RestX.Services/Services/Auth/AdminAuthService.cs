using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using RestX.BLL.DataTranferObjects.Authentication;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Auth;
using RestX.Models.Admin;

namespace RestX.BLL.Services.Auth
{
    public class AdminAuthService : IAdminAuthService
    {
        private readonly UserManager<Admin> userManager;
        private readonly SignInManager<Admin> signInManager;
        private readonly IAdminTokenService tokenService;
        private readonly IEmailService emailService;
        private readonly ILogger<AdminAuthService> logger;

        public AdminAuthService(
            UserManager<Admin> userManager,
            SignInManager<Admin> signInManager,
            IAdminTokenService tokenService,
            IEmailService emailService,
            ILogger<AdminAuthService> logger)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.tokenService = tokenService;
            this.emailService = emailService;
            this.logger = logger;
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var admin = await userManager.FindByEmailAsync(request.Email);
            if (admin == null)
                return AuthResponse.FailureResponse("Invalid email or password");

            var result = await signInManager.CheckPasswordSignInAsync(admin, request.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                return result.IsLockedOut
                    ? AuthResponse.FailureResponse("Account is locked. Please try again later.")
                    : AuthResponse.FailureResponse("Invalid email or password");
            }

            return await GenerateAuthResponseAsync(admin, "Login successful");
        }

        public async Task<AuthResponse> LogoutAsync(string adminId)
        {
            var admin = await userManager.FindByIdAsync(adminId);
            if (admin == null)
                return AuthResponse.FailureResponse("Admin not found");

            await InvalidateRefreshTokenAsync(admin);
            return AuthResponse.SuccessResponse("Logout successful");
        }

        public async Task<AuthResponse> ChangePasswordAsync(string adminId, ChangePasswordRequest request)
        {
            var admin = await userManager.FindByIdAsync(adminId);
            if (admin == null)
                return AuthResponse.FailureResponse("Admin not found");

            var result = await userManager.ChangePasswordAsync(admin, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
                return AuthResponse.FailureResponse($"Failed to change password: {FormatIdentityErrors(result)}");

            return AuthResponse.SuccessResponse("Password changed successfully");
        }

        public async Task<AuthResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var admin = await userManager.FindByEmailAsync(request.Email);
            if (admin == null)
                return AuthResponse.SuccessResponse("If the email exists, a reset link has been sent");

            var token = await userManager.GeneratePasswordResetTokenAsync(admin);
            var encodedToken = Uri.EscapeDataString(token);
            var resetLink = $"https://admin.restx.food/reset-password?email={request.Email}&token={encodedToken}";

            BackgroundJob.Enqueue(() => SendPasswordResetEmailAsync(request.Email, resetLink));

            return AuthResponse.SuccessResponse("A password reset link has been sent to your email");
        }

        public async Task SendPasswordResetEmailAsync(string email, string resetLink)
        {
            try
            {
                await emailService.SendPasswordResetLinkAsync(email, resetLink);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send password reset email to {Email}", email);
                throw;
            }
        }

        public async Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var admin = await userManager.FindByEmailAsync(request.Email);
            if (admin == null)
                return AuthResponse.FailureResponse("Invalid reset link");

            var result = await userManager.ResetPasswordAsync(admin, request.Token, request.NewPassword);
            if (!result.Succeeded)
            {
                var errors = FormatIdentityErrors(result);
                return errors.Contains("Invalid token")
                    ? AuthResponse.FailureResponse("The reset link has expired or is invalid. Please request a new one.")
                    : AuthResponse.FailureResponse($"Failed to reset password: {errors}");
            }

            return AuthResponse.SuccessResponse("Password reset successfully");
        }

        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            var admin = userManager.Users.FirstOrDefault(a => a.RefreshToken == refreshToken);
            if (admin == null || admin.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return AuthResponse.FailureResponse("Invalid or expired refresh token");

            return await GenerateAuthResponseAsync(admin, "Token refreshed successfully");
        }

        #region Private Methods
        private async Task<AuthResponse> GenerateAuthResponseAsync(Admin admin, string message)
        {
            var roles = await userManager.GetRolesAsync(admin);
            var accessToken = tokenService.GenerateAccessToken(admin, roles);
            var refreshToken = tokenService.GenerateRefreshToken();

            admin.RefreshToken = refreshToken;
            admin.RefreshTokenExpiryTime = tokenService.GetRefreshTokenExpiry();
            admin.LastLoginTime = DateTime.UtcNow;
            await userManager.UpdateAsync(admin);

            var adminInfo = new AdminInfo
            {
                Id = admin.Id,
                Email = admin.Email ?? string.Empty,
                FullName = $"{admin.FirstName} {admin.LastName}".Trim(),
                Roles = roles.ToList()
            };

            return AuthResponse.SuccessResponse(message, new AdminLoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = tokenService.GetAccessTokenExpiry(),
                Admin = adminInfo
            });
        }

        private async Task InvalidateRefreshTokenAsync(Admin admin)
        {
            admin.RefreshToken = null;
            admin.RefreshTokenExpiryTime = null;
            await userManager.UpdateAsync(admin);
        }

        private static string FormatIdentityErrors(IdentityResult result)
            => string.Join(", ", result.Errors.Select(e => e.Description));
        #endregion
    }

}
