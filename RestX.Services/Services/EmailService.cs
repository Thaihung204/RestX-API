using Microsoft.Extensions.Options;
using RestX.BLL.DataTranferObjects.Common;
using RestX.BLL.Interfaces;
using System.Net;
using System.Net.Mail;

namespace RestX.BLL.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings emailSettings;
        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            this.emailSettings = emailSettings.Value;
        }
        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpClient = new SmtpClient(emailSettings.SmtpServer)
            {
                Port = emailSettings.SmtpPort,
                Credentials = new NetworkCredential(emailSettings.Username, emailSettings.Password),
                EnableSsl = emailSettings.EnableSsl,
                Timeout = 10000
            };
            var mailMessage = new MailMessage
            {
                From = new MailAddress(emailSettings.SenderEmail, emailSettings.SenderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };
            mailMessage.To.Add(toEmail);
            await smtpClient.SendMailAsync(mailMessage);
        }
        public async Task SendPasswordResetLinkAsync(string toEmail, string resetLink)
        {
            var subject = "RestX - Password Reset Request";
            var body = EmailTemplates.PasswordReset(resetLink);
            await SendEmailAsync(toEmail, subject, body);
        }
        public async Task SendWelcomeEmployeeAsync(string toEmail, string employeeName, string setPasswordLink)
        {
            var subject = "Welcome to RestX - Set Your Password";
            var body = EmailTemplates.WelcomeEmployee(employeeName, setPasswordLink);
            await SendEmailAsync(toEmail, subject, body);
        }
    }
}
