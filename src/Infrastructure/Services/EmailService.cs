using BioLicense_Portal.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System;
using System.Threading.Tasks;

namespace BioLicense_Portal.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string to, string subject, string body, string? attachmentFileName = null, byte[]? attachmentData = null)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_config["Email:FromName"] ?? "BioLicense Portal", _config["Email:FromEmail"] ?? "noreply@biolicense.com"));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;

            var builder = new BodyBuilder();
            builder.HtmlBody = body;

            if (attachmentData != null && !string.IsNullOrEmpty(attachmentFileName))
            {
                builder.Attachments.Add(attachmentFileName, attachmentData);
            }

            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            
            var host = _config["Email:SmtpHost"];
            var port = int.Parse(_config["Email:SmtpPort"] ?? "587");
            var user = _config["Email:SmtpUsername"];
            var pass = _config["Email:SmtpPassword"];

            await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            if (!string.IsNullOrEmpty(user))
            {
                await smtp.AuthenticateAsync(user, pass);
            }
            
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}
