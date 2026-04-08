using RestX.DAL.Context;

namespace RestX.DAL.DataSeeders
{
    public class AdminSeeder : BaseUserSeeder
    {
        private const string DEFAULT_PASSWORD = "Admin@123";
        private const string DEFAULT_USERNAME = "TenantAdmin";
        private const string DEFAULT_ROLE = "Admin";
        private readonly string _tenantHostname;
        public AdminSeeder(TenantDbContext context, string tenantHostname) : base(context)
        {
            _tenantHostname = tenantHostname;
        }
        public override int Order => 4;
        protected override string SeederName => "AdminSeeder";
        protected override string Email => $"admin@{_tenantHostname}";
        protected override string Username => DEFAULT_USERNAME;
        protected override string Password => DEFAULT_PASSWORD;
        protected override string RoleName => DEFAULT_ROLE;
    }
}
