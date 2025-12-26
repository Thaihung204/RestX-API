using RestX.BLL.Interfaces;
using RestX.Models.Tenants;

namespace RestX.BLL.Services
{
    public class TenantService : BaseService, ITenantService
    {
        private readonly IRepository adminRepo;
        public TenantService(IRepository repo) : base(repo)
        {
            this.adminRepo = repo;
        }

        public async Task<IEnumerable<Tenant>> GetAllTenants()
        {
            var tenants = await adminRepo.GetAllAsync<Tenant>();
            return tenants.ToList();
        }
        public async Task<Tenant> GetTenantById(Guid id)
        {
            var tenant = await adminRepo.GetByIdAsync<Tenant>(id);
            return tenant;
        }

        public async Task<Tenant> UpsertTenant(Tenant model)
        {
            var tenant = new Tenant();
            if (tenant.Id != Guid.Empty)
            {
                tenant = await adminRepo.GetByIdAsync<Tenant>(model.Id);
                tenant.Name = model.Name;
                tenant.LogoUrl = model.LogoUrl;
                tenant.FaviconUrl = model.FaviconUrl;
                tenant.BackgroundUrl = model.BackgroundUrl;
                tenant.BaseColor = model.BaseColor;
                tenant.PrimaryColor = model.PrimaryColor;
                tenant.SecondaryColor = model.SecondaryColor;
                tenant.NetworkIp = model.NetworkIp;
                tenant.ConnectionString = model.ConnectionString;
                tenant.Status = model.Status;
                tenant.Domain = model.Domain;
                tenant.ExpiredAt = model.ExpiredAt;

                adminRepo.Update(tenant);
                
                await adminRepo.SaveAsync();
            }
            else
            {
                await adminRepo.CreateAsync(model);
            }
            return tenant;
        }
        public async Task DeleteTenant(Guid id)
        {
            var tenant = await GetTenantById(id);
            if (tenant != null)
            {
                adminRepo.Delete<Tenant>(id);
                await adminRepo.SaveAsync();
            }
        }
    }
}