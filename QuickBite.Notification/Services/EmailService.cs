using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;

namespace QuickBite.Notification.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(_config["Email:From"]));
                email.To.Add(MailboxAddress.Parse(toEmail));
                email.Subject = subject;
                
                // HTML Template with QuickBite Branding
                var htmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; border: 1px solid #eee; padding: 20px;'>
                    <h2 style='color: #FF4B2B; text-align: center;'>QuickBite</h2>
                    <hr/>
                    <p style='font-size: 16px; color: #333;'>{body}</p>
                    <br/>
                    <p style='font-size: 12px; color: #999; text-align: center;'>
                        This is an automated message from QuickBite. Please do not reply.
                    </p>
                </div>";

                email.Body = new TextPart(TextFormat.Html) { Text = htmlBody };

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(_config["Email:Host"], int.Parse(_config["Email:Port"]!), MailKit.Security.SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_config["Email:Username"], _config["Email:Password"]);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {To}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", toEmail);
            }
        }
    }
}
