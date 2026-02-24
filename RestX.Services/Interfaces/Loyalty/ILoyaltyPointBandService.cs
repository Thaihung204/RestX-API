using RestX.BLL.DataTranferObjects.Loyalty;

namespace RestX.BLL.Interfaces.Loyalty
{
    public interface ILoyaltyPointBandService
    {
        Task<IEnumerable<LoyaltyPointBand>> GetAllLoyaltyPointBands();
        Task<LoyaltyPointBand?> GetLoyaltyPointBandById(Guid id);
        Task<Guid> UpsertLoyaltyPointBand(LoyaltyPointBand item, string userId);
        Task<bool> DeleteLoyaltyPointBand(Guid id);
    }
}
