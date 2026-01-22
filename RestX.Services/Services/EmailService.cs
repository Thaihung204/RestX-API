using Microsoft.Extensions.Options;
using RestX.BLL.Interfaces;
using System.Net;
using System.Net.Mail;

namespace RestX.BLL.Services
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
    }

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
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #333;'>Password Reset Request</h2>
                        <p>You have requested to reset your password. Click the button below to reset your password:</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{resetLink}' style='background-color: #007bff; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; display: inline-block; font-weight: bold;'>Reset Password</a>
                        </div>
                        <p>Or copy and paste this link into your browser:</p>
                        <p style='word-break: break-all; color: #007bff;'>{resetLink}</p>
                        <p>This link will expire in <strong>15 minutes</strong>.</p>
                        <p>If you did not request this password reset, please ignore this email.</p>
                        <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'/>
                        <p style='color: #888; font-size: 12px;'>This is an automated message from RestX. Please do not reply.</p>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, subject, body);
        }
    }
}
