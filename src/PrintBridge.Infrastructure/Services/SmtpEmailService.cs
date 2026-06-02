using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using PrintBridge.Application.Services;
using PrintBridge.Infrastructure.Options;

namespace PrintBridge.Infrastructure.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public SmtpEmailService(IOptions<EmailSettings> options)
        {
            _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var host = _settings.SmtpServer;
            var port = _settings.SmtpPort;
            var useSsl = _settings.UseSsl;
            var user = _settings.SenderEmail;
            var pass = _settings.SenderPassword;
            var from = _settings.SenderEmail;
            var fromName = string.IsNullOrWhiteSpace(_settings.SenderName) ? "Supplier Portal" : _settings.SenderName;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, from));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = body };

            using var client = new SmtpClient();
            try
            {
                SecureSocketOptions secureSocket;
                if (useSsl)
                {
                    secureSocket = port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
                }
                else
                {
                    secureSocket = SecureSocketOptions.StartTlsWhenAvailable;
                }

                await client.ConnectAsync(host, port, secureSocket);

                if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(pass))
                {
                    await client.AuthenticateAsync(user, pass);
                }

                await client.SendAsync(message);
            }
            finally
            {
                try { await client.DisconnectAsync(true); } catch {}
            }
        }
    }
}
