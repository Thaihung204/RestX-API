using RestX.BLL.DataTranferObjects.Loyalty;

namespace RestX.BLL.Interfaces.Loyalty
{
    public interface ILoyaltyPointBandService
    {
        Task<IEnumerable<LoyaltyPointBandItem>> GetAllLoyaltyPointBands();
        Task<LoyaltyPointBandItem?> GetLoyaltyPointBandById(Guid id);
        Task<Guid> UpsertLoyaltyPointBand(LoyaltyPointBandItem item, string userId);
        Task<bool> DeleteLoyaltyPointBand(Guid id);
    }
}
