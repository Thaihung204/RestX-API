using AutoMapper;
using Microsoft.AspNetCore.Identity;
using RestX.BLL.DataTranferObjects.Authentication;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Auth;
using RestX.Models.Customers;
using RestX.Models.Identity;
using RestX.Models.Tenants;

namespace RestX.BLL.Services.Auth
{
    public class AuthService : BaseService, IAuthService
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly RoleManager<IdentityRole<Guid>> roleManager;
        private readonly ITokenService tokenService;
        private readonly IAuthLinkService authLinkService;
        private readonly IEmailService emailService;
        private readonly IMapper mapper;
        private const string CustomerRole = "Customer";

        public AuthService(
            IRepository repo,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            ITokenService tokenService,
            IAuthLinkService authLinkService,
            IEmailService emailService,
            IRedisService redisService,
            IMapper mapper,
            IEnumerable<ActiveTenant> tenant = null!) : base(repo, redisService, tenant)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.roleManager = roleManager;
            this.tokenService = tokenService;
            this.authLinkService = authLinkService;
            this.emailService = emailService;
            this.mapper = mapper;
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
            await InvalidateRefreshTokenAsync(user);
            return AuthResponse.SuccessResponse("Logout successful");
        }

        public async Task<AuthResponse> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return AuthResponse.FailureResponse("User not found");
            var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
                return AuthResponse.FailureResponse($"Failed to change password: {FormatIdentityErrors(result)}");
            user.LastModified = DateTime.UtcNow;
            await userManager.UpdateAsync(user);
            return AuthResponse.SuccessResponse("Password changed successfully");
        }

        public async Task<AuthResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return AuthResponse.FailureResponse("Please check your email and try again");
            try
            {
                var baseUrl = GetTenantBaseUrl();
                var resetLink = await authLinkService.GeneratePasswordResetLinkAsync(request.Email, baseUrl);
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
            var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
            if (!result.Succeeded)
            {
                var errors = FormatIdentityErrors(result);
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
            var user = FindUserByRefreshToken(refreshToken);
            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return AuthResponse.FailureResponse("Invalid or expired refresh token");
            return await GenerateAuthResponseAsync(user, "Token refreshed successfully");
        }

        public async Task<CheckPhoneResponse> CheckPhoneNumberAsync(string phoneNumber)
        {
            var normalizedPhone = NormalizePhoneNumber(phoneNumber);
            var user = FindUserByPhoneNumber(normalizedPhone);
            if (user == null)
                return new CheckPhoneResponse { Exists = false };
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
            var user = FindUserByPhoneNumber(normalizedPhone);
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
            if (FindUserByPhoneNumber(normalizedPhone) != null)
                return AuthResponse.FailureResponse("Phone number already registered");
            var user = CreatePhoneUser(request.FullName, normalizedPhone);
            var result = await userManager.CreateAsync(user);
            if (!result.Succeeded)
                return AuthResponse.FailureResponse($"Failed to create user: {FormatIdentityErrors(result)}");
            await EnsureRoleAndAssignAsync(user, CustomerRole);
            var customer = await CreateCustomerAsync(user.Id);
            return await GenerateAuthResponseAsync(user, "Registration and login successful", customer.Id);
        }

        #region Private Methods
        private async Task<AuthResponse> GenerateAuthResponseAsync(ApplicationUser user, string message, Guid? customerId = null)
        {
            var roles = await userManager.GetRolesAsync(user);
            var accessToken = tokenService.GenerateAccessToken(user, roles, CurrentTenant?.Hostname);
            var refreshToken = tokenService.GenerateRefreshToken();
            await UpdateUserTokensAsync(user, refreshToken);
            var userInfo = mapper.Map<UserInfo>(user);
            userInfo.Roles = roles.ToList();
            userInfo.CustomerId = customerId;
            return AuthResponse.SuccessResponse(message, new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = tokenService.GetAccessTokenExpiry(),
                User = userInfo
            });
        }

        private async Task UpdateUserTokensAsync(ApplicationUser user, string refreshToken)
        {
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = tokenService.GetRefreshTokenExpiry();
            user.LastLoginTime = DateTime.UtcNow;
            await userManager.UpdateAsync(user);
        }

        private async Task InvalidateRefreshTokenAsync(ApplicationUser user)
        {
            user.RefreshToken = string.Empty;
            user.RefreshTokenExpiryTime = null;
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

        private string GetTenantBaseUrl()
            => $"https://{CurrentTenant?.Hostname ?? "localhost"}";

        private ApplicationUser? FindUserByRefreshToken(string refreshToken)
            => userManager.Users.FirstOrDefault(u => u.RefreshToken == refreshToken);

        private ApplicationUser? FindUserByPhoneNumber(string phoneNumber)
            => userManager.Users.FirstOrDefault(u => u.PhoneNumber == phoneNumber);

        private static ApplicationUser CreatePhoneUser(string fullName, string phoneNumber)
            => new()
            {
                Id = Guid.NewGuid(),
                UserName = fullName,
                NormalizedUserName = fullName.ToUpper(),
                PhoneNumber = phoneNumber,
                PhoneNumberConfirmed = true,
                EmailConfirmed = false,
                LastModified = DateTime.UtcNow,
                RefreshToken = string.Empty,
                SecurityStamp = Guid.NewGuid().ToString()
            };

        private static string NormalizePhoneNumber(string phoneNumber)
            => phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

        private static string FormatIdentityErrors(IdentityResult result)
            => string.Join(", ", result.Errors.Select(e => e.Description));
        #endregion
    }
}
