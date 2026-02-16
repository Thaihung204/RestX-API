using RestX.BLL.DataTranferObjects.Floor;

namespace RestX.BLL.Interfaces.Tables
{
    public interface IFloorService
    {
        Task<IEnumerable<FloorItem>> GetAllFloors();
        Task<FloorItem?> GetFloorById(Guid id);
        Task<FloorItem> UpsertFloor(FloorItem request, string? currentUser = null);
        Task<bool> DeleteFloor(Guid id);
        Task<FloorLayoutResponse?> GetFloorLayout(Guid floorId);
        Task<bool> SaveLayout(Guid floorId, SaveLayoutRequest request, string? currentUser = null);
    }
}
