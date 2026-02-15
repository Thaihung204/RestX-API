using RestX.BLL.DataTranferObjects.Inventory;

namespace RestX.BLL.Interfaces.Inventory
{
    public interface ISupplierService
    {
        Task<IEnumerable<SupplierItem>> GetAllSuppliers();
        Task<SupplierItem?> GetSupplierById(Guid id);
        Task<Guid> UpsertSupplier(SupplierItem supplierItem);
        Task DeleteSupplier(Guid id);
    }
}