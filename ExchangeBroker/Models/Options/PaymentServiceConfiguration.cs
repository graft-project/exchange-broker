namespace ExchangeBroker.Models.Options
{
    public class PaymentServiceConfiguration
    {
        public decimal ExchangeBrokerFee { get; set; }
        public int PaymentTimeoutMinutes { get; set; }
        public double MaxServiceProviderFee { get; set; }
    }
}
