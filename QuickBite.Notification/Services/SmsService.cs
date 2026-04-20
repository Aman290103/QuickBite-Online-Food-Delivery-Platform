using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace QuickBite.Notification.Services
{
    public class SmsService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SmsService> _logger;

        public SmsService(IConfiguration config, ILogger<SmsService> logger)
        {
            _config = config;
            _logger = logger;
            TwilioClient.Init(_config["Twilio:AccountSid"], _config["Twilio:AuthToken"]);
        }

        public async Task SendSmsAsync(string toPhone, string message)
        {
            try
            {
                await MessageResource.CreateAsync(
                    body: message,
                    from: new Twilio.Types.PhoneNumber(_config["Twilio:FromNumber"]),
                    to: new Twilio.Types.PhoneNumber(toPhone)
                );
                _logger.LogInformation("SMS sent successfully to {To}", toPhone);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS to {To}", toPhone);
            }
        }
    }
}
