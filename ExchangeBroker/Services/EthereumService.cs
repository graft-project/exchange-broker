using ExchangeBroker.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace ExchangeBroker.Services
{
    public class EthereumService : IEthereumService
    {
        private static AccountPoolService AccountPoolService { get; set; }
        private static EthereumLib.TransactionManager TransactionManager { get; set; }
        private static ILogger logger;

        public static void Init(string DBConnectionString, ILoggerFactory loggerFactory, bool isTestnet, string defaultAccountPassword, string gethNodeAddress, string drainAddress, decimal drainLimit)
        {
            AccountPoolService accountPoolService = new AccountPoolService(DBConnectionString, loggerFactory);

            TransactionManager = new EthereumLib.TransactionManager(accountPoolService, isTestnet, defaultAccountPassword, gethNodeAddress, drainAddress, drainLimit);

            AccountPoolService = accountPoolService;

            logger = loggerFactory.CreateLogger<EthereumService>();
        }

        public async Task CheckExchange(Exchange exchange)
        {
            try
            {
                decimal addedValue = 0;

                var lastHash = await AccountPoolService.GetLastTransactionHash(exchange.PayWalletAddress);

                var status = await TransactionManager.GetTransactionsByAddress(exchange.PayWalletAddress);

                if (status == null)
                    return;

                var lastRecord = status.Result.OrderByDescending(x => x.BlockNumber).FirstOrDefault();

                if(lastRecord == null || lastRecord.Hash == lastHash)
                {
                    return;
                }

                if(lastRecord.IsError == 1)
                {
                    exchange.Status = Graft.Infrastructure.PaymentStatus.Fail;
                }
                else
                {
                    exchange.Status = Graft.Infrastructure.PaymentStatus.Received;
                    exchange.ReceivedConfirmations = (int)lastRecord.Confirmations;
                    if(long.TryParse(lastRecord.Value, out long val))
                    {
                        addedValue = EthereumLib.Converter.AtomicToDecimal(val);
                        exchange.ReceivedAmount = addedValue;
                    }
                }

                await AccountPoolService.SetInactive(exchange.PayWalletAddress, lastRecord.Hash, addedValue);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Etherium Check Exchange failed");
                throw;
            }
        }

        public async Task CheckPayment(Payment payment)
        {
            try
            {
                decimal addedBalance = 0;

                var lastHash = await AccountPoolService.GetLastTransactionHash(payment.PayWalletAddress);

                var status = await TransactionManager.GetTransactionsByAddress(payment.PayWalletAddress);

                if (status == null)
                    return;

                var lastRecord = status.Result.OrderByDescending(x => x.BlockNumber).FirstOrDefault();

                if (lastRecord == null || lastRecord.Hash == lastHash)
                {
                    return;
                }

                if (lastRecord.IsError == 1)
                {
                    payment.Status = Graft.Infrastructure.PaymentStatus.Fail;
                }
                else
                {
                    payment.Status = Graft.Infrastructure.PaymentStatus.Received;
                    payment.ReceivedConfirmations = (int)lastRecord.Confirmations;
                    if (long.TryParse(lastRecord.Value, out long val))
                    {
                        addedBalance = EthereumLib.Converter.AtomicToDecimal(val);
                        payment.ReceivedAmount = addedBalance;
                    }
                }

                await AccountPoolService.SetInactive(payment.PayWalletAddress, lastRecord.Hash, addedBalance);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Etherium Check Payment failed");
                throw;
            }
        }

        public async Task CreateAddress(Payment payment)
        {
            var address = await TransactionManager.GetPoolOrNewAddress(new TimeSpan(0, 0, 3));
            payment.PayWalletAddress = address.Address;
        }

        public async Task CreateAddress(Exchange exchange)
        {
            var address = await TransactionManager.GetPoolOrNewAddress(new TimeSpan(0,0,3));
            exchange.PayWalletAddress = address.Address;
        }

        public string GetUri(Payment payment)
        {
            return $"ethereum:{payment.PayWalletAddress}?value={string.Format("{0:#.0#e-00}", EthereumLib.Converter.DecimalToAtomicUnit(payment.PayAmount))}";
        }

        public string GetUri(Exchange exchange)
        {
            return $"ethereum:{exchange.PayWalletAddress}?value={string.Format("{0:#.0#e-00}", EthereumLib.Converter.DecimalToAtomicUnit(exchange.SellAmount))}";
        }
    }
}
