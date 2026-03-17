using RestX.BLL.DataTranferObjects.Combo;

namespace RestX.BLL.Interfaces
{
    public interface IComboService
    {
        Task<ComboSearchResult> GetAllCombos(ComboSearch result);
        Task<ComboSummary> GetComboById(Guid id);
        Task<Guid> UpsertCombo(ComboSummary comboSummary);
        Task<bool> DeleteCombo(Guid id);
        Task<List<ComboSummary>> GetActiveCombos();
    }
}