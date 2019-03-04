namespace ExchangeBroker.Models.Options
{
    public class ExchangeServiceConfiguration
    {
        public decimal ExchangeBrokerFee { get; set; }
        public int PaymentTimeoutMinutes { get; set; }
        public string DapiUrl { get; set; }
        public string WalletUrl { get; set; }
        public string IncomeGraftWalletAddress { get; set; }
        public string StableCoinContractAddress { get; set; }
        public string EthereumAddress { get; set; }
        public string EthereumPrivatekey { get; set; }
        public string EthereumUrl { get; set; }
    }
}
