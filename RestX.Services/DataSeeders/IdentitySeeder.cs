//using Microsoft.AspNetCore.Identity;
//using Microsoft.EntityFrameworkCore;
//using RestX.Models.Identity;

//namespace RestX.BLL.DataSeeders
//{

//    public class IdentitySeeder
//    {
//        private readonly UserManager<ApplicationUser> _userManager;
//        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
//        private readonly string _tenantHostname;

//        public IdentitySeeder(
//            UserManager<ApplicationUser> userManager,
//            RoleManager<IdentityRole<Guid>> roleManager,
//            string tenantHostname)
//        {
//            _userManager = userManager;
//            _roleManager = roleManager;
//            _tenantHostname = tenantHostname;
//        }

//        public async Task SeedAsync()
//        {
//            await SeedRolesAsync();
//            await SeedAdminUserAsync();
//        }

//        private async Task SeedRolesAsync()
//        {
//            var roles = new[]
//            {
//                "Admin",         
//                "Manager",       
//                "Chef",           
//                "Waiter",         
//                "Cashier",        
//                "Receptionist"    
//            };

//            foreach (var roleName in roles)
//            {
//                if (!await _roleManager.RoleExistsAsync(roleName))
//                {
//                    var role = new IdentityRole<Guid>
//                    {
//                        Id = Guid.NewGuid(),
//                        Name = roleName,
//                        NormalizedName = roleName.ToUpper()
//                    };
//                    await _roleManager.CreateAsync(role);
//                }
//            }
//        }

//        private async Task SeedAdminUserAsync()
//        {
//            var adminEmail = $"admin@{_tenantHostname}";

//            var existingAdmin = await _userManager.FindByEmailAsync(adminEmail);
//            if (existingAdmin != null)
//            {
//                return;
//            }

//            var adminUser = new ApplicationUser
//            {
//                Id = Guid.NewGuid(),
//                UserName = "admin",
//                Email = adminEmail,
//                NormalizedUserName = "ADMIN",
//                NormalizedEmail = adminEmail.ToUpper(),
//                EmailConfirmed = true,
//                PhoneNumberConfirmed = true,
//                SecurityStamp = Guid.NewGuid().ToString(),
//                RefreshToken = string.Empty
//            };

//            var result = await _userManager.CreateAsync(adminUser, "Admin@123");

//            if (result.Succeeded)
//            {
//                await _userManager.AddToRoleAsync(adminUser, "Admin");
//            }
//        }
//    }
//}
