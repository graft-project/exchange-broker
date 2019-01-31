using ExchangeBroker.Data;
using ExchangeBroker.Extensions;
using ExchangeBroker.Models;
using ExchangeBroker.Models.Options;
using Graft.Infrastructure;
using Graft.Infrastructure.Broker;
using Graft.Infrastructure.Rate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ExchangeBroker.Services
{
    public class ExchangeService : IExchangeService
    {
        readonly ILogger _logger;
        readonly ApplicationDbContext _db;
        readonly IRateCache _rateCache;
        readonly IMemoryCache _cache;
        readonly ICryptoProviderService _cryptoProviderService;
        readonly IGraftWalletService _wallet;
        readonly PaymentServiceConfiguration _settings;

        public ExchangeService(ILoggerFactory loggerFactory,
            ApplicationDbContext db,
            IConfiguration configuration,
            IRateCache rateCache,
            IMemoryCache cache,
            ICryptoProviderService cryptoProviderService,
            IGraftWalletService wallet)
        {
            _settings = configuration
               .GetSection("PaymentService")
               .Get<PaymentServiceConfiguration>();

            _logger = loggerFactory.CreateLogger(nameof(ExchangeService));
            _db = db;
            _rateCache = rateCache;
            _cache = cache;
            _cryptoProviderService = cryptoProviderService;
            _wallet = wallet;
        }

        public async Task<BrokerExchangeResult> Exchange(BrokerExchangeParams model)
        {
            _logger.LogInformation("Exchange: {@params}", model);

            ValidateExchangeParams(model);

            Exchange exchange = await ExchangeCalculator.Create(model, _rateCache, _settings);

            await _cryptoProviderService.CreateAddress(exchange);
            exchange.Log($"Created new {model.SellCurrency} address: {exchange.PayWalletAddress}");

            _cache.Set(exchange.ExchangeId, exchange, DateTimeOffset.Now.AddMinutes(_settings.PaymentTimeoutMinutes));

            _db.Exchange.Add(exchange);
            await _db.SaveChangesAsync();

            var res = GetExchangeResult(exchange);
            _logger.LogInformation("Exchange Result: {@params}", res);
            return res;
        }

        void ValidateExchangeParams(BrokerExchangeParams model)
        {
            if (!string.IsNullOrWhiteSpace(model.FiatCurrency))
            {
                if (model.BuyFiatAmount <= 0 && model.SellFiatAmount <= 0)
                    throw new ApiException(ErrorCode.FiatAmountEmpty);

                if (model.SellFiatAmount > 0 && model.BuyFiatAmount > 0)
                    throw new ApiException(ErrorCode.OnlyOneAmountAllowed);

                if (model.SellAmount > 0 || model.BuyAmount > 0)
                    throw new ApiException(ErrorCode.OnlyOneAmountAllowed);
            }
            else
            {
                if (model.SellAmount > 0 && model.BuyAmount > 0)
                    throw new ApiException(ErrorCode.OnlyOneAmountAllowed);

                if (model.SellAmount <= 0 && model.BuyAmount <= 0)
                    throw new ApiException(ErrorCode.InvalidAmount);
            }

            if (string.IsNullOrWhiteSpace(model.SellCurrency))
                throw new ApiException(ErrorCode.SellCurrencyEmpty);

            if (!_rateCache.IsSupported(model.SellCurrency))
                throw new ApiException(ErrorCode.SellCurrencyNotSupported, model.SellCurrency);

            if (string.IsNullOrWhiteSpace(model.BuyCurrency))
                throw new ApiException(ErrorCode.BuyCurrencyEmpty);

            if (model.BuyCurrency != "GRFT")
                throw new ApiException(ErrorCode.BuyCurrencyNotSupported, model.BuyCurrency);

            if (string.IsNullOrWhiteSpace(model.WalletAddress))
                throw new ApiException(ErrorCode.WalletEmpty);
        }

        public async Task<BrokerExchangeResult> ExchangeStatus(string exchangeId)
        {
            _logger.LogInformation("ExchangeStatus: {@params}", exchangeId);

            if (string.IsNullOrEmpty(exchangeId))
                throw new ApiException(ErrorCode.ExchangeIdEmpty);

            _cache.TryGetValue(exchangeId, out Exchange exchange);
            if (exchange == null)
            {
                exchange = await _db.Exchange.FirstOrDefaultAsync(t => t.ExchangeId == exchangeId);
                if (exchange == null)
                    throw new ApiException(ErrorCode.ExchangeNotFoundOrExpired);
            }


            //todo - move this check into a background thread
            {
                if (await _cryptoProviderService.CheckExchange(exchange))
                {
                    if (exchange.Status == PaymentStatus.Received)
                    {
                        exchange.Log($"Received Payment of {exchange.ReceivedAmount} {exchange.SellCurrency}");

                        // received amount can be different from initial amount, 
                        // so we need to recalculate all amounts
                        ExchangeCalculator.Recalc(exchange, _settings);

                        await _wallet.ProcessExchange(exchange, _db);
                    }

                    _db.Exchange.Update(exchange);
                    await _db.SaveChangesAsync();
                }
            }

            var res = GetExchangeResult(exchange);
            _logger.LogInformation("ExchangeStatus Result: {@params}", res);
            return res;
        }

        BrokerExchangeResult GetExchangeResult(Exchange exchange)
        {
            var address = _cryptoProviderService.GetUri(exchange);

            return new BrokerExchangeResult()
            {
                ExchangeId = exchange.ExchangeId,
                Status = exchange.Status,

                SellCurrency = exchange.SellCurrency,
                SellAmount = exchange.SellAmount,

                BuyCurrency = exchange.BuyCurrency,
                BuyAmount = exchange.BuyAmount,

                SellToUsdRate = exchange.SellToUsdRate,
                GraftToUsdRate = exchange.GraftToUsdRate,

                ExchangeBrokerFeeRate = _settings.ExchangeBrokerFee,
                ExchangeBrokerFeeAmount = exchange.ExchangeBrokerFee,

                PayWalletAddress = address,

                ProcessingEvents = exchange.ProcessingEvents
            };
        }
    }
}
