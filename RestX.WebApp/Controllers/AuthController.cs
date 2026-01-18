using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestX.BLL.DTOs.Auth;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Auth;
using RestX.WebApp.Controllers.BaseControllers;
using System.Security.Claims;

namespace RestX.WebApp.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : BaseController
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService, IExceptionHandler exceptionHandler) : base(exceptionHandler)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.LoginAsync(request);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                exceptionHandler.RaiseException(ex);
                return BadRequest(AuthResponseDto.FailureResponse("An internal error occurred"));
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(AuthResponseDto.FailureResponse("User not authenticated"));
                }

                var result = await _authService.LogoutAsync(userId.Value);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                exceptionHandler.RaiseException(ex);
                return BadRequest(AuthResponseDto.FailureResponse("An internal error occurred"));
            }
        }


        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
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
                    return Unauthorized(AuthResponseDto.FailureResponse("User not authenticated"));
                }

                var result = await _authService.ChangePasswordAsync(userId.Value, request);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                exceptionHandler.RaiseException(ex);
                return BadRequest(AuthResponseDto.FailureResponse("An internal error occurred"));
            }
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.ForgotPasswordAsync(request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                exceptionHandler.RaiseException(ex);
                return BadRequest(AuthResponseDto.FailureResponse("An internal error occurred"));
            }
        }


        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.ResetPasswordAsync(request);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                exceptionHandler.RaiseException(ex);
                return BadRequest(AuthResponseDto.FailureResponse("An internal error occurred"));
            }
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterCustomer([FromBody] RegisterCustomerRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.RegisterCustomerAsync(request);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                exceptionHandler.RaiseException(ex);
                return BadRequest(AuthResponseDto.FailureResponse("An internal error occurred"));
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
                    return BadRequest(AuthResponseDto.FailureResponse("Refresh token is required"));
                }

                var result = await _authService.RefreshTokenAsync(request.RefreshToken);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                exceptionHandler.RaiseException(ex);
                return BadRequest(AuthResponseDto.FailureResponse("An internal error occurred"));
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
    }

    public class RefreshTokenRequestDto
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}
