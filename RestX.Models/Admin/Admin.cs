using Microsoft.AspNetCore.Identity;
using RestX.Models.BaseModel;

namespace RestX.Models.Admin;

public partial class Admin : IdentityUser
{
    public string FirstName { get; set; }

    public string LastName { get; set; }
}
