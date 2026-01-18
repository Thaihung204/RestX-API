using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RestX.BLL.DTOs.Auth;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Auth;
using RestX.Models.Customers;
using RestX.Models.Identity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace RestX.BLL.Services
{
    public class AuthService : BaseService, IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly IEmailService _emailService;
        private readonly IRedisService _redisService;
        private readonly IRepository _repo;
        private readonly AppSettings _appSettings;

        private const int PASSWORD_RESET_TOKEN_EXPIRY_MINUTES = 15;
        private const int ACCESS_TOKEN_EXPIRY_HOURS = 1;
        private const int REFRESH_TOKEN_EXPIRY_DAYS = 7;

        public AuthService(
            IRepository repo,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IEmailService emailService,
            IRedisService redisService,
            IOptions<AppSettings> appSettings) : base(repo)
        {
            _repo = repo;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _emailService = emailService;
            _redisService = redisService;
            _appSettings = appSettings.Value;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return AuthResponseDto.FailureResponse("Invalid email or password");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                {
                    return AuthResponseDto.FailureResponse("Account is locked. Please try again later.");
                }
                return AuthResponseDto.FailureResponse("Invalid email or password");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = GenerateJwtToken(user, roles);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(REFRESH_TOKEN_EXPIRY_DAYS);
            user.LastLoginTime = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var response = new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(ACCESS_TOKEN_EXPIRY_HOURS),
                User = new UserInfoDto
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FullName = user.UserName ?? string.Empty,
                    PhoneNumber = user.PhoneNumber,
                    Roles = roles.ToList()
                }
            };

            return AuthResponseDto.SuccessResponse("Login successful", response);
        }

        public async Task<AuthResponseDto> LogoutAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return AuthResponseDto.FailureResponse("User not found");
            }

            user.RefreshToken = null!;
            user.RefreshTokenExpiryTime = null;
            await _userManager.UpdateAsync(user);

            return AuthResponseDto.SuccessResponse("Logout successful");
        }

        public async Task<AuthResponseDto> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return AuthResponseDto.FailureResponse("User not found");
            }

            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return AuthResponseDto.FailureResponse($"Failed to change password: {errors}");
            }

            user.LastModified = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            return AuthResponseDto.SuccessResponse("Password changed successfully");
        }

        public async Task<AuthResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return AuthResponseDto.SuccessResponse("If the email exists, a password reset link has been sent");
            }

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = Uri.EscapeDataString(resetToken);
            var encodedEmail = Uri.EscapeDataString(request.Email);

            var resetLink = $"{_appSettings.FrontendUrl}/reset-password?email={encodedEmail}&token={encodedToken}";

            try
            {
                await _emailService.SendPasswordResetLinkAsync(request.Email, resetLink);
            }
            catch
            {
                return AuthResponseDto.FailureResponse("Failed to send password reset email. Please try again later.");
            }

            return AuthResponseDto.SuccessResponse("A password reset link has been sent to your email");
        }

        public async Task<AuthResponseDto> ResetPasswordAsync(ResetPasswordRequestDto request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return AuthResponseDto.FailureResponse("Invalid reset link");
            }

            var decodedToken = Uri.UnescapeDataString(request.Token);
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                if (errors.Contains("Invalid token"))
                {
                    return AuthResponseDto.FailureResponse("The reset link has expired or is invalid. Please request a new one.");
                }
                return AuthResponseDto.FailureResponse($"Failed to reset password: {errors}");
            }

            user.LastModified = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            return AuthResponseDto.SuccessResponse("Password reset successfully");
        }

        public async Task<AuthResponseDto> RegisterCustomerAsync(RegisterCustomerRequestDto request)
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return AuthResponseDto.FailureResponse("Email already exists");
            }

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = request.FullName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                EmailConfirmed = true,
                LastModified = DateTime.UtcNow,
                RefreshToken = string.Empty
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return AuthResponseDto.FailureResponse($"Failed to create user: {errors}");
            }

            const string customerRole = "Customer";
            if (!await _roleManager.RoleExistsAsync(customerRole))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(customerRole));
            }
            await _userManager.AddToRoleAsync(user, customerRole);

            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                ApplicationUserId = user.Id,
                MembershipLevel = "BRONZE",
                LoyaltyPoints = 0,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            await _repo.CreateAsync(customer);

            return AuthResponseDto.SuccessResponse("Registration successful", new
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = request.FullName
            });
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
        {
            var user = await GetUserByRefreshToken(refreshToken);
            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return AuthResponseDto.FailureResponse("Invalid or expired refresh token");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var newAccessToken = GenerateJwtToken(user, roles);
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(REFRESH_TOKEN_EXPIRY_DAYS);
            await _userManager.UpdateAsync(user);

            var response = new LoginResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(ACCESS_TOKEN_EXPIRY_HOURS),
                User = new UserInfoDto
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FullName = user.UserName ?? string.Empty,
                    PhoneNumber = user.PhoneNumber,
                    Roles = roles.ToList()
                }
            };

            return AuthResponseDto.SuccessResponse("Token refreshed successfully", response);
        }

        private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_appSettings.Secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: null,
                audience: null,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(ACCESS_TOKEN_EXPIRY_HOURS),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private async Task<ApplicationUser?> GetUserByRefreshToken(string refreshToken)
        {
            var users = _userManager.Users.Where(u => u.RefreshToken == refreshToken).ToList();
            return await Task.FromResult(users.FirstOrDefault());
        }
    }
}
