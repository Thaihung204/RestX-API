using Microsoft.AspNetCore.Identity;

namespace RestX.Models.Admin;

public partial class Admin : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiryTime { get; set; }

    public DateTime? LastLoginTime { get; set; }
}
