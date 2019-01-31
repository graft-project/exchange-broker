using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExchangeBroker.Models;

namespace ExchangeBroker.Services
{
    public class CryptoProviderService : ICryptoProviderService
    {
        private Dictionary<string, ICryptoService> registeredServices;

        public CryptoProviderService(IBitcoinService bitcoinService, IEthereumService ethereumService)
        {
            registeredServices = new Dictionary<string, ICryptoService>();

            registeredServices.Add("BTC", bitcoinService);
            registeredServices.Add("ETH", ethereumService);
        }

        public Task<bool> CheckExchange(Exchange exchange)
        {
            return GetService(exchange.SellCurrency).CheckExchange(exchange);
        }

        public Task CheckPayment(Payment payment)
        {
            return GetService(payment.PayCurrency).CheckPayment(payment);
        }

        public Task CreateAddress(Payment payment)
        {
            return GetService(payment.PayCurrency).CreateAddress(payment);
        }

        public Task CreateAddress(Exchange exchange)
        {
            return GetService(exchange.SellCurrency).CreateAddress(exchange);
        }

        public string GetUri(Payment payment)
        {
            return GetService(payment.PayCurrency).GetUri(payment);
        }

        public string GetUri(Exchange exchange)
        {
            return GetService(exchange.SellCurrency).GetUri(exchange);
        }

        public ICryptoService GetService(string currency)
        {
            if (registeredServices.ContainsKey(currency))
            {
                return registeredServices[currency];
            }

            throw new NullReferenceException($"Service for {currency} is currently missing.");
        }
    }
}
