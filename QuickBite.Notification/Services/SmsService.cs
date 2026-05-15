using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace QuickBite.Notification.Services
{
    // [SERVICE: SMS DISPATCH]
    // This service handles the delivery of text messages (SMS) using Twilio.
    public class SmsService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SmsService> _logger;

        public SmsService(IConfiguration config, ILogger<SmsService> logger)
        {
            _config = config;
            _logger = logger;
            
            // Initialize Twilio client with credentials from configuration
            TwilioClient.Init(_config["Twilio:AccountSid"], _config["Twilio:AuthToken"]);
        }

        // [METHOD: SEND SMS]
        // Sends a text message to the specified phone number.
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
