using RestX.BLL.DataTranferObjects.Common;
using RestX.Models.Identity;

namespace RestX.BLL.Interfaces.Auth
{
    public interface IUserAccountService
    {
        Task<ApplicationUser> CreateUserAsync(CreateUserRequest request);
        Task<ApplicationUser> UpdateUserAsync(Guid userId, UpdateUserRequest request);
        Task DeactivateUserAsync(ApplicationUser user);
        Task<bool> EmailExistsAsync(string email);
        Task<ApplicationUser?> GetUserByMemberIdAsync(Guid memberId);
        Task<IList<string>> GetUserRolesAsync(ApplicationUser user);
    }
}
