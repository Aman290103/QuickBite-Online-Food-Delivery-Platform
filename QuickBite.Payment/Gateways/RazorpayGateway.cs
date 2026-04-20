using Razorpay.Api;

namespace QuickBite.Payment.Gateways
{
    public class RazorpayGateway
    {
        private readonly IConfiguration _config;
        private readonly RazorpayClient _client;

        public RazorpayGateway(IConfiguration config)
        {
            _config = config;
            string key = "rzp_test_SfiZQYAPtU4oMA";
            string secret = "4ihkNi4cElnaOOjdTs8uNSSQ";
            _client = new RazorpayClient(key, secret);
        }

        public string CreateOrder(decimal amount, string receipt)
        {
            Dictionary<string, object> options = new Dictionary<string, object>();
            options.Add("amount", (int)(amount * 100)); // amount in the smallest currency unit
            options.Add("receipt", receipt);
            options.Add("currency", "INR");
            
            Order order = _client.Order.Create(options);
            return order["id"].ToString();
        }

        public bool VerifySignature(string paymentId, string orderId, string signature)
        {
            try
            {
                Dictionary<string, string> attributes = new Dictionary<string, string>();
                attributes.Add("razorpay_payment_id", paymentId);
                attributes.Add("razorpay_order_id", orderId);
                attributes.Add("razorpay_signature", signature);

                Utils.verifyPaymentSignature(attributes);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string Refund(string paymentId, decimal amount)
        {
            Dictionary<string, object> options = new Dictionary<string, object>();
            options.Add("amount", (int)(amount * 100));
            
            Refund refund = _client.Payment.Fetch(paymentId).Refund(options);
            return refund["id"].ToString();
        }
    }
}
