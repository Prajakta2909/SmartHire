using Microsoft.Extensions.Options;
using SmartHire.Models;
using System.Net;
using System.Net.Mail;

namespace SmartHire.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(
            IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailAsync(
            string toEmail,
            string subject,
            string body)
        {
            using var client = new SmtpClient(
                _emailSettings.SmtpServer,
                _emailSettings.Port);

            client.Credentials = new NetworkCredential(
                _emailSettings.Username,
                _emailSettings.Password);

            client.EnableSsl = true;

            using var message = new MailMessage
            {
                From = new MailAddress(
                    _emailSettings.SenderEmail,
                    _emailSettings.SenderName),

                Subject = subject,

                Body = body,

                IsBodyHtml = true
            };

            message.To.Add(toEmail);

            await client.SendMailAsync(message);
        }
    }
}