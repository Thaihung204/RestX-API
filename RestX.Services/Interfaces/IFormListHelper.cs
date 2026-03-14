using RestX.BLL.DataTranferObjects.Share;

namespace RestX.BLL.Interfaces
{
    public interface IFormListHelper
    {
        Task<List<SelectOption>> GetListByName(string name);
        List<SelectOption> ConvertEnumToList(Type type);
        string StringValueOfEnum(Enum value);
    }
}