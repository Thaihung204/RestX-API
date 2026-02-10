using QRCoder;
using RestX.BLL.Interfaces;

namespace RestX.BLL.Services
{
    public class QRCodeService : IQRCodeService
    {
        public string GenerateTableQRCode(Guid tableId, string tenantHostname)
        {
            var url = $"https://{tenantHostname}/customer/{tableId}";
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
                using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
                {
                    byte[] qrCodeBytes = qrCode.GetGraphic(20);
                    string base64String = Convert.ToBase64String(qrCodeBytes);
                    return $"data:image/png;base64,{base64String}";
                }
            }
        }
    }
}
