namespace RestX.BLL.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task SendPasswordResetLinkAsync(string toEmail, string resetLink);
        Task SendWelcomeEmployeeAsync(string toEmail, string employeeName, string setPasswordLink);
    }
}
