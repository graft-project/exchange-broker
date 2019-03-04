using ExchangeBroker.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExchangeBroker.Services
{
    public class CryptoProviderService : ICryptoProviderService
    {
        private Dictionary<string, ICryptoService> registeredServices;

        public CryptoProviderService(IBitcoinService bitcoinService, IEthereumService ethereumService)
        {
            registeredServices = new Dictionary<string, ICryptoService>
            {
                { "BTC", bitcoinService },
                { "ETH", ethereumService }
            };
        }

        public Task<bool> CheckExchange(Exchange exchange)
        {
            return GetService(exchange.SellCurrency).CheckExchange(exchange);
        }

        public Task CreateAddress(Exchange exchange)
        {
            return GetService(exchange.SellCurrency).CreateAddress(exchange);
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
