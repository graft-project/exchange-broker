using ExchangeBroker.Extensions;
using ExchangeBroker.Models;
using ExchangeBroker.Models.Options;
using Graft.Infrastructure;
using Graft.Infrastructure.Broker;
using Graft.Infrastructure.Rate;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExchangeBroker.Services
{
    public class ExchangeToStableCalculator
    {
        internal static async Task<Exchange> Create(BrokerExchangeToStableParams model, 
            IRateCache rateCache, ExchangeServiceConfiguration settings)
        {
            var log = new List<EventItem>
            {
                new EventItem($"Started exchange calculation")
            };

            log.Add(new EventItem($"Requesting GRFT rate..."));
            decimal graftRate = await rateCache.GetRateToUsd("GRFT");
            log.Add(new EventItem($"Received GRFT rate: {graftRate} USD"));

            decimal fee = settings.ExchangeBrokerFee;
            decimal usdAmount = graftRate * model.SellAmount;
            decimal feeAmount = usdAmount * fee;
            decimal buyerAmount = usdAmount - feeAmount;

            var exchange = new Exchange
            {
                ExchangeId = model.ExchangeId ?? Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow,
                Status = PaymentStatus.Waiting,

                SellAmount = model.SellAmount,
                SellCurrency = model.SellCurrency,

                BuyAmount = buyerAmount,
                BuyCurrency = "USDT",

                SellToUsdRate = 1M,
                GraftToUsdRate = graftRate,

                ExchangeBrokerFee = feeAmount,

                BuyerWallet = model.WalletAddress,

                OutTxStatus = PaymentStatus.New
            };

            exchange.ProcessingEvents = log;

            log.Add(new EventItem($"Created exchange: {exchange.SellAmount} {exchange.SellCurrency} to {exchange.BuyAmount} {exchange.BuyCurrency}"));
            log.Add(new EventItem($"Exchange Broker fee of {fee*100}% is {exchange.ExchangeBrokerFee} GRFT"));

            return exchange;
        }

        internal static void Recalc(Exchange exchange, ExchangeServiceConfiguration settings)
        {
            decimal sellRate = exchange.SellToUsdRate;
            decimal graftRate = exchange.GraftToUsdRate;
            decimal sellAmount = 0;
            decimal graftAmount = 0;
            decimal buyerAmount = 0;
            decimal feeAmount = 0;
            decimal fee = settings.ExchangeBrokerFee;

            sellAmount = exchange.ReceivedAmount;
            graftAmount = sellAmount * sellRate / graftRate;
            feeAmount = graftAmount * fee;
            buyerAmount = graftAmount - feeAmount;

            exchange.SellAmount = sellAmount;
            exchange.BuyAmount = buyerAmount;
            exchange.ExchangeBrokerFee = feeAmount;

            exchange.Log($"Recalcalated amounts: {exchange.SellAmount} {exchange.SellCurrency} to {exchange.BuyAmount} {exchange.BuyCurrency}");
        }
    }
}
