namespace RestX.BLL.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task SendPasswordResetLinkAsync(string toEmail, string resetLink);
        Task SendWelcomeEmployeeAsync(string toEmail, string employeeName, string setPasswordLink);
        Task SendTenantRequestSubmittedAsync(string toEmail, string businessName, string submittedAt);
        Task SendTenantRequestAcceptedAsync(string toEmail, string businessName, string hostname);
        Task SendTenantRequestRejectedAsync(string toEmail, string businessName);
    }
}
