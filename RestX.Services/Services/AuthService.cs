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
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly RoleManager<IdentityRole<Guid>> roleManager;
        private readonly IEmailService emailService;
        private readonly IRedisService redisService;
        private readonly IRepository repo;
        private readonly AppSettings appSettings;

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
            this.repo = repo;
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.roleManager = roleManager;
            this.emailService = emailService;
            this.redisService = redisService;
            this.appSettings = appSettings.Value;
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return AuthResponse.FailureResponse("Invalid email or password");
            }

            var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                {
                    return AuthResponse.FailureResponse("Account is locked. Please try again later.");
                }
                return AuthResponse.FailureResponse("Invalid email or password");
            }

            var roles = await userManager.GetRolesAsync(user);
            var accessToken = GenerateJwtToken(user, roles);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(REFRESH_TOKEN_EXPIRY_DAYS);
            user.LastLoginTime = DateTime.UtcNow;
            await userManager.UpdateAsync(user);

            var response = new LoginResponse
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

            return AuthResponse.SuccessResponse("Login successful", response);
        }

        public async Task<AuthResponse> LogoutAsync(Guid userId)
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return AuthResponse.FailureResponse("User not found");
            }

            user.RefreshToken = null!;
            user.RefreshTokenExpiryTime = null;
            await userManager.UpdateAsync(user);

            return AuthResponse.SuccessResponse("Logout successful");
        }

        public async Task<AuthResponse> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return AuthResponse.FailureResponse("User not found");
            }

            var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return AuthResponse.FailureResponse($"Failed to change password: {errors}");
            }

            user.LastModified = DateTime.UtcNow;
            await userManager.UpdateAsync(user);

            return AuthResponse.SuccessResponse("Password changed successfully");
        }

        public async Task<AuthResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return AuthResponse.SuccessResponse("If the email exists, a password reset link has been sent");
            }

            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = Uri.EscapeDataString(resetToken);
            var encodedEmail = Uri.EscapeDataString(request.Email);

            var resetLink = $"{appSettings.FrontendUrl}/reset-password?email={encodedEmail}&token={encodedToken}";

            try
            {
                await emailService.SendPasswordResetLinkAsync(request.Email, resetLink);
            }
            catch
            {
                return AuthResponse.FailureResponse("Failed to send password reset email. Please try again later.");
            }

            return AuthResponse.SuccessResponse(
        "A password reset link has been sent to your email",
        new
        {
            Email = request.Email,
            Token = encodedToken
        }
    );
        }

        public async Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return AuthResponse.FailureResponse("Invalid reset link");
            }

            var decodedToken = Uri.UnescapeDataString(request.Token);
            var result = await userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                if (errors.Contains("Invalid token"))
                {
                    return AuthResponse.FailureResponse("The reset link has expired or is invalid. Please request a new one.");
                }
                return AuthResponse.FailureResponse($"Failed to reset password: {errors}");
            }

            user.LastModified = DateTime.UtcNow;
            await userManager.UpdateAsync(user);

            return AuthResponse.SuccessResponse("Password reset successfully");
        }

        public async Task<AuthResponse> RegisterCustomerAsync(RegisterCustomerRequest request)
        {
            var existingUser = await userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return AuthResponse.FailureResponse("Email already exists");
            }

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = request.Email,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                EmailConfirmed = true,
                LastModified = DateTime.UtcNow,
                RefreshToken = string.Empty
            };

            var result = await userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return AuthResponse.FailureResponse($"Failed to create user: {errors}");
            }

            const string customerRole = "Customer";
            if (!await roleManager.RoleExistsAsync(customerRole))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(customerRole));
            }
            await userManager.AddToRoleAsync(user, customerRole);

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

            try
            {
                await repo.CreateAsync(customer);
                await repo.SaveAsync();
            }
            catch
            {
                await userManager.DeleteAsync(user);
                throw;
            }


            return AuthResponse.SuccessResponse("Registration successful", new
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = request.FullName
            });
        }

        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            var user = await GetUserByRefreshToken(refreshToken);
            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return AuthResponse.FailureResponse("Invalid or expired refresh token");
            }

            var roles = await userManager.GetRolesAsync(user);
            var newAccessToken = GenerateJwtToken(user, roles);
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(REFRESH_TOKEN_EXPIRY_DAYS);
            await userManager.UpdateAsync(user);

            var response = new LoginResponse
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

            return AuthResponse.SuccessResponse("Token refreshed successfully", response);
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

            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(appSettings.Secret));
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
            var users = userManager.Users.Where(u => u.RefreshToken == refreshToken).ToList();
            return await Task.FromResult(users.FirstOrDefault());
        }
    }
}
