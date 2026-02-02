using RestX.Models.Identity;

namespace RestX.BLL.Interfaces.Auth
{
    public interface IAuthLinkService
    {
        Task<string> GeneratePasswordResetLinkAsync(string email, string baseUrl);
        Task<string> GenerateWelcomeLinkAsync(ApplicationUser user, string baseUrl);
    }
}
