namespace RestX.BLL.Interfaces
{
    public interface IQRCodeService
    {
        string GenerateTableQRCode(Guid tableId, string tenantHostname);
    }
}
