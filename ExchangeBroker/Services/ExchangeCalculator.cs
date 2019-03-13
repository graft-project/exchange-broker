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
    public class ExchangeCalculator
    {
        internal static async Task<Exchange> Create(BrokerExchangeParams model, 
            IRateCache rateCache, ExchangeServiceConfiguration settings)
        {
            var log = new List<EventItem>
            {
                new EventItem($"Started exchange calculation")
            };

            log.Add(new EventItem($"Requesting {model.SellCurrency} rate..."));
            decimal sellRate = await rateCache.GetRateToUsd(model.SellCurrency);
            log.Add(new EventItem($"Received {model.SellCurrency} rate: {sellRate} USD"));

            log.Add(new EventItem($"Requesting GRFT rate..."));
            decimal graftRate = await rateCache.GetRateToUsd("GRFT");
            log.Add(new EventItem($"Received GRFT rate: {graftRate} USD"));

            decimal sellAmount = 0;
            decimal graftAmount = 0;
            decimal buyerAmount = 0;
            decimal feeAmount = 0;
            decimal fee = settings.ExchangeBrokerFee;

            if (!string.IsNullOrWhiteSpace(model.FiatCurrency))
            {
                if (model.SellFiatAmount > 0)
                {
                    sellAmount = model.SellFiatAmount / sellRate;
                    graftAmount = model.SellFiatAmount / graftRate;
                    feeAmount = graftAmount * fee;
                    buyerAmount = graftAmount - feeAmount;
                }
                else
                {
                    buyerAmount = model.BuyFiatAmount / graftRate;
                    graftAmount = buyerAmount / (1 - fee);
                    feeAmount = graftAmount - buyerAmount;
                    sellAmount = graftAmount * graftRate / sellRate;
                }
            }
            else
            {
                if (model.SellAmount > 0)
                {
                    sellAmount = model.SellAmount;
                    graftAmount = sellAmount * sellRate / graftRate;
                    feeAmount = graftAmount * fee;
                    buyerAmount = graftAmount - feeAmount;
                }
                else
                {
                    buyerAmount = model.BuyAmount;
                    graftAmount = buyerAmount / (1 - fee);
                    feeAmount = graftAmount - buyerAmount;
                    sellAmount = graftAmount * graftRate / sellRate;
                }
            }

            var exchange = new Exchange
            {
                ExchangeId = model.PaymentId ?? Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow,
                //ExchangeStatus = PaymentStatus.Waiting,

                SellAmount = sellAmount,
                SellCurrency = model.SellCurrency,

                BuyAmount = buyerAmount,
                BuyCurrency = model.BuyCurrency,

                SellToUsdRate = sellRate,
                GraftToUsdRate = graftRate,

                ExchangeBrokerFee = feeAmount,

                BuyerWallet = model.WalletAddress,

                InTxStatus = PaymentStatus.Waiting,
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
