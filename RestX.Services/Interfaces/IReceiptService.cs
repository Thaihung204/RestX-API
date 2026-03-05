namespace RestX.BLL.Interfaces
{
    public interface IReceiptService
    {
        Task<byte[]> GenerateReceiptAsync(Guid paymentId);
    }
}
