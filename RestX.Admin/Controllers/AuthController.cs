using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestX.Admin.Controllers.BaseControllers;
using RestX.BLL.DataTranferObjects.Authentication;
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

        public AuthController(IAdminAuthService adminAuthService, IExceptionHandler exceptionHandler)
            : base(exceptionHandler)
        {
            this.adminAuthService = adminAuthService;
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

                return Ok(result);
            }
            catch (Exception ex)
            {
                exceptionHandler.RaiseException(ex);
                return BadRequest(AuthResponse.FailureResponse("An internal error occurred"));
            }
        }

        private string? GetCurrentAdminId()
            => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

}
