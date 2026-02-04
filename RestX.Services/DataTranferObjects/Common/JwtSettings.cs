namespace RestX.BLL.DataTranferObjects.Common
{
    public class JwtSettings
    {
        public string Secret { get; set; } = string.Empty;
        public int AccessTokenExpiryMinutes { get; set; } = 60;
        public int RefreshTokenExpiryDays { get; set; } = 7;
        public string? Issuer { get; set; }
        public string? Audience { get; set; }
    }
}
