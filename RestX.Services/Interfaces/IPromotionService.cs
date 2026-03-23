using RestX.BLL.DataTranferObjects.Promotion;

namespace RestX.BLL.Interfaces
{
    public interface IPromotionService
    {
        Task<List<Promotion>> GetAllPromotions();
        Task<List<Promotion>> GetActivePromotions();
        Task<Promotion> GetPromotionById(Guid id);
        Task<Promotion?> GetPromotionByCode(string code);
        Task<Guid> UpsertPromotion(Promotion item);
        Task<bool> DeletePromotion(Guid id);
    }
}