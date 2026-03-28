using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RestX.Admin.Controllers.BaseControllers;
using RestX.BLL.DataTranferObjects.Authentication;
using RestX.BLL.DataTranferObjects.Common;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Auth;
using System.Security.Claims;

namespace RestX.Admin.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : BaseController
    {
        private readonly IAdminAuthService adminAuthService;
        private readonly JwtSettings jwtSettings;

        public AuthController(IAdminAuthService adminAuthService, IExceptionHandler exceptionHandler, IOptions<JwtSettings> jwtSettings)
            : base(exceptionHandler)
        {
            this.adminAuthService = adminAuthService;
            this.jwtSettings = jwtSettings.Value;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await adminAuthService.LoginAsync(request);
                if (!result.Success)
                    return BadRequest(result);

                if (result.Data is AdminLoginResponse loginData)
                    SetAuthCookies(loginData);
                return Ok(result);
            }
            catch (Exception ex)
            {
                exceptionHandler.RaiseException(ex);
                return BadRequest(AuthResponse.FailureResponse("An internal error occurred"));
            }
        }

        [HttpPost("logout")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var adminId = GetCurrentAdminId();
                if (adminId == null)
                    return Unauthorized(AuthResponse.FailureResponse("Admin not authenticated"));

                var result = await adminAuthService.LogoutAsync(adminId);
                if (!result.Success)
                    return BadRequest(result);

                ClearAuthCookies();
                return Ok(result);
            }
            catch (Exception ex)
            {
                exceptionHandler.RaiseException(ex);
                return BadRequest(AuthResponse.FailureResponse("An internal error occurred"));
            }
        }

        [HttpPost("change-password")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var adminId = GetCurrentAdminId();
                if (adminId == null)
                    return Unauthorized(AuthResponse.FailureResponse("Admin not authenticated"));

                var result = await adminAuthService.ChangePasswordAsync(adminId, request);
                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                exceptionHandler.RaiseException(ex);
                return BadRequest(AuthResponse.FailureResponse("An internal error occurred"));
            }
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await adminAuthService.ForgotPasswordAsync(request);
                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                exceptionHandler.RaiseException(ex);
                return BadRequest(AuthResponse.FailureResponse("An internal error occurred"));
            }
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await adminAuthService.ResetPasswordAsync(request);
                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                exceptionHandler.RaiseException(ex);
                return BadRequest(AuthResponse.FailureResponse("An internal error occurred"));
            }
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.RefreshToken))
                    return BadRequest(AuthResponse.FailureResponse("Refresh token is required"));

                var result = await adminAuthService.RefreshTokenAsync(request.RefreshToken);
                if (!result.Success)
                    return BadRequest(result);

                if (result.Data is AdminLoginResponse refreshData)
                    UpdateAccessTokenCookie(refreshData);
                return Ok(result);
            }
            catch (Exception ex)
            {
                exceptionHandler.RaiseException(ex);
                return BadRequest(AuthResponse.FailureResponse("An internal error occurred"));
            }
        }

        #region Helpers

        private string? GetCurrentAdminId()
            => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        private void SetAuthCookies(AdminLoginResponse loginData)
        {
            Response.Cookies.Append("admin_access_token", loginData.AccessToken,
                new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.None, Expires = loginData.ExpiresAt });
            Response.Cookies.Append("admin_refresh_token", loginData.RefreshToken,
                new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.None, Expires = DateTimeOffset.UtcNow.AddDays(jwtSettings.RefreshTokenExpiryDays) });
        }

        private void UpdateAccessTokenCookie(AdminLoginResponse loginData)
        {
            Response.Cookies.Append("admin_access_token", loginData.AccessToken,
                new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.None, Expires = loginData.ExpiresAt });
        }

        private void ClearAuthCookies()
        {
            Response.Cookies.Delete("admin_access_token");
            Response.Cookies.Delete("admin_refresh_token");
        }

        #endregion
    }

}
