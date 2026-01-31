using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RestX.BLL.DataTranferObjects.Auth;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Auth;
using RestX.Models.Customers;
using RestX.Models.Identity;
using RestX.Models.Tenants;
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
        private readonly IMapper mapper;
        private readonly AppSettings appSettings;
        private const int ACCESS_TOKEN_EXPIRY_HOURS = 1;
        private const int REFRESH_TOKEN_EXPIRY_DAYS = 7;
        private const string CUSTOMER_ROLE = "Customer";
        public AuthService(
            IRepository repo,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IEmailService emailService,
            IRedisService redisService,
            IMapper mapper,
            IOptions<AppSettings> appSettings,
            IEnumerable<ActiveTenant> tenant = null) : base(repo, redisService, tenant)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.roleManager = roleManager;
            this.emailService = emailService;
            this.mapper = mapper;
            this.appSettings = appSettings.Value;
        }
        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return AuthResponse.FailureResponse("Invalid email or password");
            var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                return result.IsLockedOut
                    ? AuthResponse.FailureResponse("Account is locked. Please try again later.")
                    : AuthResponse.FailureResponse("Invalid email or password");
            }
            return await GenerateAuthResponseAsync(user, "Login successful");
        }
        public async Task<AuthResponse> LogoutAsync(Guid userId)
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return AuthResponse.FailureResponse("User not found");

            user.RefreshToken = null!;
            user.RefreshTokenExpiryTime = null;
            await userManager.UpdateAsync(user);

            return AuthResponse.SuccessResponse("Logout successful");
        }
        public async Task<AuthResponse> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return AuthResponse.FailureResponse("User not found");
            var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
                return AuthResponse.FailureResponse($"Failed to change password: {GetIdentityErrors(result)}");

            user.LastModified = DateTime.UtcNow;
            await userManager.UpdateAsync(user);

            return AuthResponse.SuccessResponse("Password changed successfully");
        }
        public async Task<AuthResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return AuthResponse.SuccessResponse("If the email exists, a password reset link has been sent");
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = BuildTenantLink("reset-password", request.Email, resetToken);
            try
            {
                await emailService.SendPasswordResetLinkAsync(request.Email, resetLink);
            }
            catch
            {
                return AuthResponse.FailureResponse("Failed to send password reset email. Please try again later.");
            }

            return AuthResponse.SuccessResponse("A password reset link has been sent to your email");
        }
        public async Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return AuthResponse.FailureResponse("Invalid reset link");
            var decodedToken = Uri.UnescapeDataString(request.Token);
            var result = await userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);
            if (!result.Succeeded)
            {
                var errors = GetIdentityErrors(result);
                return errors.Contains("Invalid token")
                    ? AuthResponse.FailureResponse("The reset link has expired or is invalid. Please request a new one.")
                    : AuthResponse.FailureResponse($"Failed to reset password: {errors}");
            }
            user.LastModified = DateTime.UtcNow;
            await userManager.UpdateAsync(user);
            return AuthResponse.SuccessResponse("Password reset successfully");
        }
        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            var user = await GetUserByRefreshTokenAsync(refreshToken);
            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return AuthResponse.FailureResponse("Invalid or expired refresh token");

            return await GenerateAuthResponseAsync(user, "Token refreshed successfully");
        }
        public async Task<CheckPhoneResponse> CheckPhoneNumberAsync(string phoneNumber)
        {
            var normalizedPhone = NormalizePhoneNumber(phoneNumber);
            var user = userManager.Users.FirstOrDefault(u => u.PhoneNumber == normalizedPhone);
            if (user == null)
            {
                return new CheckPhoneResponse
                {
                    Exists = false,
                    CustomerName = null,
                    CustomerId = null
                };
            }
            var customer = await Repo.GetFirstAsync<Customer>(c => c.ApplicationUserId == user.Id);
            return new CheckPhoneResponse
            {
                Exists = true,
                CustomerName = user.UserName,
                CustomerId = customer?.Id
            };
        }
        public async Task<AuthResponse> CustomerPhoneLoginAsync(CustomerPhoneLoginRequest request)
        {
            var normalizedPhone = NormalizePhoneNumber(request.PhoneNumber);
            var user = userManager.Users.FirstOrDefault(u => u.PhoneNumber == normalizedPhone);
            if (user == null)
                return AuthResponse.FailureResponse("Phone number not registered");
            var customer = await Repo.GetFirstAsync<Customer>(c => c.ApplicationUserId == user.Id);
            if (customer == null || !customer.IsActive)
                return AuthResponse.FailureResponse("Customer account is inactive or not found");

            return await GenerateAuthResponseAsync(user, "Login successful", customer.Id);
        }
        public async Task<AuthResponse> CustomerPhoneRegisterAsync(CustomerPhoneRegisterRequest request)
        {
            var normalizedPhone = NormalizePhoneNumber(request.PhoneNumber);
            var existingUser = userManager.Users.FirstOrDefault(u => u.PhoneNumber == normalizedPhone);
            if (existingUser != null)
                return AuthResponse.FailureResponse("Phone number already registered");
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = request.FullName,
                NormalizedUserName = request.FullName.ToUpper(),
                PhoneNumber = normalizedPhone,
                PhoneNumberConfirmed = true,
                EmailConfirmed = false,
                LastModified = DateTime.UtcNow,
                RefreshToken = string.Empty,
                SecurityStamp = Guid.NewGuid().ToString()
            };
            var result = await userManager.CreateAsync(user);
            if (!result.Succeeded)
                return AuthResponse.FailureResponse($"Failed to create user: {GetIdentityErrors(result)}");
            await EnsureRoleAndAssignAsync(user, CUSTOMER_ROLE);
            var customer = await CreateCustomerAsync(user.Id);
            return await GenerateAuthResponseAsync(user, "Registration and login successful", customer.Id);
        }
        private async Task<AuthResponse> GenerateAuthResponseAsync(ApplicationUser user, string message, Guid? customerId = null)
        {
            var roles = await userManager.GetRolesAsync(user);
            var accessToken = GenerateJwtToken(user, roles);
            var refreshToken = GenerateRefreshToken();
            await UpdateUserTokensAsync(user, refreshToken);
            var userInfo = mapper.Map<UserInfo>(user);
            userInfo.Roles = roles.ToList();
            userInfo.CustomerId = customerId;
            var response = new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(ACCESS_TOKEN_EXPIRY_HOURS),
                User = userInfo
            };
            return AuthResponse.SuccessResponse(message, response);
        }
        private async Task UpdateUserTokensAsync(ApplicationUser user, string refreshToken)
        {
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(REFRESH_TOKEN_EXPIRY_DAYS);
            user.LastLoginTime = DateTime.UtcNow;
            await userManager.UpdateAsync(user);
        }
        private async Task EnsureRoleAndAssignAsync(ApplicationUser user, string roleName)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
                await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));

            await userManager.AddToRoleAsync(user, roleName);
        }
        private async Task<Customer> CreateCustomerAsync(Guid userId)
        {
            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                ApplicationUserId = userId,
                MembershipLevel = "BRONZE",
                LoyaltyPoints = 0,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };
            try
            {
                await Repo.CreateAsync(customer);
                return customer;
            }
            catch
            {
                var user = await userManager.FindByIdAsync(userId.ToString());
                if (user != null)
                    await userManager.DeleteAsync(user);
                throw;
            }
        }
        private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(ClaimTypes.Name, user.UserName ?? string.Empty),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
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
        private async Task<ApplicationUser?> GetUserByRefreshTokenAsync(string refreshToken)
        {
            var user = userManager.Users.FirstOrDefault(u => u.RefreshToken == refreshToken);
            return await Task.FromResult(user);
        }
        private string BuildTenantLink(string path, string email, string token)
        {
            var baseUrl = $"https://{CurrentTenant?.Hostname ?? "localhost"}";
            var encodedEmail = Uri.EscapeDataString(email);
            var encodedToken = Uri.EscapeDataString(token);
            return $"{baseUrl}/{path}?email={encodedEmail}&token={encodedToken}";
        }
        private static string NormalizePhoneNumber(string phoneNumber)
            => phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        private static string GetIdentityErrors(IdentityResult result)
            => string.Join(", ", result.Errors.Select(e => e.Description));
    }
}
