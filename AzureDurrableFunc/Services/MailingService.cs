using InvoiceGenerator.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvoiceGenerator.Services
{
    public interface IMailingService
    {
        Task SendInvoiceNotificationAsync(string recipientEmail, string fileName, string emailBody);
    }

    internal class MailingService : IMailingService
    {
        private readonly SMTPOptions _options;
        private readonly ILogger<MailingService> _logger;

        public MailingService(IOptions<SMTPOptions> options, ILogger<MailingService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }
     
        public async Task SendInvoiceNotificationAsync(string recipientEmail, string fileName, string emailBody)
        {
            try
            {
                var message = new MimeMessage();

                message.From.Add(new MailboxAddress(
                    _options.FromName,
                    _options.FromAddress
                ));

                message.To.Add(new MailboxAddress("", recipientEmail));

                message.Subject = $"New invoice: {fileName}";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
                    <html>
                    <body>
                       {emailBody}
                    </body>
                    </html>
                "
                };


                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();

                await client.ConnectAsync(
                   _options.SmtpHost,
                   _options.SmtpPort,
                   SecureSocketOptions.Auto  
                );

                await client.AuthenticateAsync(
                    _options.Username,
                    _options.Password
                );

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email has been successfully sent to {Email} для файлу {FileName}",
                    recipientEmail, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured while sending email to {Email}", recipientEmail);
                throw;
            }
        }
    }
}