using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestX.BLL.DataTranferObjects.Authentication;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Auth;
using RestX.Models.Identity;
using RestX.Models.Tenants;
using RestX.WebApp.Controllers.BaseControllers;
using System.Security.Claims;

namespace RestX.WebApp.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : BaseController
    {
        private readonly IAuthService authService;
        public AuthController(IAuthService authService,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            IExceptionHandler exceptionHandler,
            IEnumerable<ActiveTenant> tenant) : base(mapper, userManager, exceptionHandler, tenant)
        {
            this.authService = authService;
        }
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var result = await authService.LoginAsync(request);
                if (!result.Success)
                {
                    return BadRequest(result);
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest(AuthResponse.FailureResponse("An internal error occurred"));
            }
        }
        [HttpPost("logout")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(AuthResponse.FailureResponse("User not authenticated"));
                }
                var result = await authService.LogoutAsync(userId.Value);
                if (!result.Success)
                {
                    return BadRequest(result);
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
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
                {
                    return BadRequest(ModelState);
                }
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(AuthResponse.FailureResponse("User not authenticated"));
                }
                var result = await authService.ChangePasswordAsync(userId.Value, request);

                if (!result.Success)
                {
                    return BadRequest(result);
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
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
                {
                    return BadRequest(ModelState);
                }
                var result = await authService.ForgotPasswordAsync(request);
                if (!result.Success)
                {
                    return BadRequest(result);
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
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
                {
                    return BadRequest(ModelState);
                }
                var result = await authService.ResetPasswordAsync(request);
                if (!result.Success)
                {
                    return BadRequest(result);
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest(AuthResponse.FailureResponse("An internal error occurred"));
            }
        }
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.RefreshToken))
                {
                    return BadRequest(AuthResponse.FailureResponse("Refresh token is required"));
                }
                var result = await authService.RefreshTokenAsync(request.RefreshToken);
                if (!result.Success)
                {
                    return BadRequest(result);
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest(AuthResponse.FailureResponse("An internal error occurred"));
            }
        }
        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return null;
            }
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
        [HttpPost("customer/check-phone")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckPhoneNumber([FromBody] CheckPhoneRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var result = await authService.CheckPhoneNumberAsync(request.PhoneNumber);
                return Ok(result);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest(AuthResponse.FailureResponse("An internal error occurred"));
            }
        }
        [HttpPost("customer/phone-login")]
        [AllowAnonymous]
        public async Task<IActionResult> CustomerPhoneLogin([FromBody] CustomerPhoneLoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var result = await authService.CustomerPhoneLoginAsync(request);
                if (!result.Success)
                {
                    return BadRequest(result);
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest(AuthResponse.FailureResponse("An internal error occurred"));
            }
        }
        [HttpPost("customer/phone-register")]
        [AllowAnonymous]
        public async Task<IActionResult> CustomerPhoneRegister([FromBody] CustomerPhoneRegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var result = await authService.CustomerPhoneRegisterAsync(request);
                if (!result.Success)
                {
                    return BadRequest(result);
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                this.ExceptionHandler.RaiseException(ex);
                return BadRequest(AuthResponse.FailureResponse("An internal error occurred"));
            }
        }
    }
    public class RefreshTokenRequestDto
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}