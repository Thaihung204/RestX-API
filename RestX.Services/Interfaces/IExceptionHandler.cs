namespace RestX.BLL.Interfaces
{
    public interface IExceptionHandler
    {
        void RaiseException(Exception ex, string customMessage = "");
    }
}
