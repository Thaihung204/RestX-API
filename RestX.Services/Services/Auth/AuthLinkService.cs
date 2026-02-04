using Microsoft.AspNetCore.Identity;
using RestX.BLL.Interfaces.Auth;
using RestX.Models.Identity;

namespace RestX.BLL.Services.Auth
{
    public class AuthLinkService : IAuthLinkService
    {
        private readonly UserManager<ApplicationUser> userManager;

        public AuthLinkService(UserManager<ApplicationUser> userManager)
        {
            this.userManager = userManager;
        }

        public async Task<string> GeneratePasswordResetLinkAsync(string email, string baseUrl)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
                throw new InvalidOperationException("User not found");

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            return BuildLink(baseUrl, "reset-password", email, token);
        }

        public async Task<string> GenerateWelcomeLinkAsync(ApplicationUser user, string baseUrl)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            return BuildLink(baseUrl, "set-password", user.Email!, token);
        }

        private static string BuildLink(string baseUrl, string path, string email, string token)
        {
            return $"{baseUrl}/{path}?email={email}&token={token}";
        }
    }
}
