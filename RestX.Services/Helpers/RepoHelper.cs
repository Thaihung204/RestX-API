using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestX.AdminDAL.Context;
using RestX.BLL.Interfaces;
using RestX.BLL.Services;
using RestX.DAL.Context;
using RestX.Models.Tenants;

namespace RestX.BLL.Helpers
{
    public class RepoHelper : IRepoHelper
    {
        private IServiceScopeFactory serviceScopeFactory;
        private IEnumerable<IHttpContextAccessor> context;
        private readonly IBackgroundJobClient jobClient;
        private readonly IRedisService cache;

        public RepoHelper(IEnumerable<IHttpContextAccessor> context, IServiceScopeFactory serviceScopeFactory, IBackgroundJobClient jobClient, IRedisService cache)
        {
            this.context = context;
            this.serviceScopeFactory = serviceScopeFactory;
            this.jobClient = jobClient;
            this.cache = cache;
        }

        public EntityFrameworkRepository<TenantDbContext> GetTenantRepo(ActiveTenant activeTenant)
        {
            var repo = new EntityFrameworkRepository<TenantDbContext>(
                new TenantDbContext(activeTenant, this.context), jobClient, cache, activeTenant);

            return repo;
        }

        public async Task<ActiveTenant?> GetAllActiveTenantsObject(Guid? tenantId = null)
        {
            using var scope = serviceScopeFactory.CreateScope();
            var adminContext = scope.ServiceProvider.GetRequiredService<RestxAdminContext>();

            var query = adminContext.Tenants.AsQueryable();

            if (tenantId.HasValue)
            {
                query = query.Where(t => t.Id == tenantId.Value);
            }

            var tenant = await query
                .Select(t => new ActiveTenant
                {
                    Id = t.Id,
                    Name = t.Name,
                    ConnectionString = t.ConnectionString
                })
                .FirstOrDefaultAsync();

            return tenant;
        }
    }
}
