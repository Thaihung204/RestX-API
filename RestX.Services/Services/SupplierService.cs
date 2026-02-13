using AutoMapper;
using RestX.BLL.DataTranferObjects.Inventory;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Inventory;
using RestX.Models.Inventory;
using RestX.Models.Tenants;

namespace RestX.BLL.Services
{
    public class SupplierService : BaseService, ISupplierService
    {
        private readonly IMapper mapper;

        public SupplierService(
            IMapper mapper,
            IRepository repo,
            IRedisService redisService,
            IEnumerable<ActiveTenant> tenant = null
        ) : base(repo, redisService, tenant)
        {
            this.mapper = mapper;
        }

        public async Task<IEnumerable<SupplierItem>> GetAllSuppliers()
        {
            var suppliers = (await Repo.GetAllAsync<Supplier>(
                orderBy: q => q.OrderBy(s => s.Name)
            )).ToList();

            return mapper.Map<List<SupplierItem>>(suppliers);
        }

        public async Task<SupplierItem?> GetSupplierById(Guid id)
        {
            var supplier = await Repo.GetOneAsync<Supplier>(filter: s => s.Id == id);
            return mapper.Map<SupplierItem>(supplier);
        }

        public async Task<Guid> UpsertSupplier(SupplierItem supplierItem)
        {
            Supplier supplier;

            if (supplierItem.Id != null)
            {
                supplier = await Repo.GetByIdAsync<Supplier>(supplierItem.Id);
                if (supplier == null)
                    return Guid.Empty;

                supplier.Name = supplierItem.Name;
                supplier.Phone = supplierItem.Phone;
                supplier.Email = supplierItem.Email;
                supplier.Address = supplierItem.Address;
                supplier.IsActive = supplierItem.IsActive;

                Repo.Update(supplier);
                await Repo.SaveAsync();
                return supplier.Id;
            }

            supplier = new Supplier
            {
                Name = supplierItem.Name,
                Phone = supplierItem.Phone,
                Email = supplierItem.Email,
                Address = supplierItem.Address,
                IsActive = supplierItem.IsActive
            };

            await Repo.CreateAsync(supplier);

            return supplier.Id;
        }

        public async Task DeleteSupplier(Guid id)
        {
            var supplier = await Repo.GetByIdAsync<Supplier>(id);
            if (supplier == null)
                return;

            Repo.Delete<Supplier>(id);
            await Repo.SaveAsync();
        }
    }
}