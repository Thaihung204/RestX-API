using Microsoft.Extensions.Logging;
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
        private readonly ILogger<EmailService>? logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService>? logger = null)
        {
            this.emailSettings = emailSettings.Value;
            this.logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            using var smtpClient = CreateSmtpClient();
            using var mailMessage = CreateMailMessage(toEmail, subject, body);

            try
            {
                await smtpClient.SendMailAsync(mailMessage);
                logger?.LogInformation("Email sent successfully to {Email}", toEmail);
            }
            catch (SmtpException ex)
            {
                logger?.LogError(ex, "Failed to send email to {Email}", toEmail);
                throw new InvalidOperationException($"Failed to send email: {ex.Message}", ex);
            }
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

        private SmtpClient CreateSmtpClient()
        {
            return new SmtpClient(emailSettings.SmtpServer)
            {
                Port = emailSettings.SmtpPort,
                Credentials = new NetworkCredential(emailSettings.Username, emailSettings.Password),
                EnableSsl = emailSettings.EnableSsl,
                Timeout = 30000
            };
        }

        private MailMessage CreateMailMessage(string toEmail, string subject, string body)
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(emailSettings.SenderEmail, emailSettings.SenderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);
            return mailMessage;
        }
    }
}
