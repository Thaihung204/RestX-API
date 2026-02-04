using RestX.DAL.Context;

namespace RestX.DAL.DataSeeders
{
    public class SystemAdminSeeder : BaseUserSeeder
    {
        private const string SYSTEM_ADMIN_EMAIL = "admin@restx.food";
        private const string SYSTEM_ADMIN_PASSWORD = "Admin@123";
        private const string SYSTEM_ADMIN_USERNAME = "SystemAdmin";
        private const string SYSTEM_ADMIN_ROLE = "System Admin";

        public SystemAdminSeeder(TenantDbContext context) : base(context)
        {
        }
        public override int Order => 3;
        protected override string SeederName => "SystemAdminSeeder";
        protected override string Email => SYSTEM_ADMIN_EMAIL;
        protected override string Username => SYSTEM_ADMIN_USERNAME;
        protected override string Password => SYSTEM_ADMIN_PASSWORD;
        protected override string RoleName => SYSTEM_ADMIN_ROLE;
    }
}
