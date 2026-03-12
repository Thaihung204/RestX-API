using RestX.BLL.DataTranferObjects.Floor;

namespace RestX.BLL.Interfaces.Tables
{
    public interface IFloorService
    {
        Task<IEnumerable<Floor>> GetAllFloors();
        Task<Floor?> GetFloorById(Guid id);
        Task<Guid> UpsertFloor(Floor request, string? currentUser = null);
        Task<bool> DeleteFloor(Guid id);
        Task<FloorLayoutResponse?> GetFloorLayout(Guid floorId, DateTime? at = null);
        Task<bool> SaveLayout(Guid floorId, SaveLayoutRequest request, string? currentUser = null);
    }
}
