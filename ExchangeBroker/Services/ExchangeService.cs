using ExchangeBroker.Data;
using ExchangeBroker.Extensions;
using ExchangeBroker.Models;
using ExchangeBroker.Models.Options;
using Graft.DAPI;
using Graft.Infrastructure;
using Graft.Infrastructure.Broker;
using Graft.Infrastructure.Rate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WalletRpc;

namespace ExchangeBroker.Services
{
    public class ExchangeService : IExchangeService
    {
        readonly ILogger _logger;
        readonly ApplicationDbContext _db;
        readonly IRateCache _rateCache;
        readonly IMemoryCache _cache;
        readonly ICryptoProviderService _cryptoProviderService;
        readonly GraftDapi _dapi;
        readonly WalletPool _walletPool;
        readonly ExchangeServiceConfiguration _settings;

        public ExchangeService(ILoggerFactory loggerFactory,
            ApplicationDbContext db,
            IConfiguration configuration,
            IRateCache rateCache,
            IMemoryCache cache,
            ICryptoProviderService cryptoProviderService,
            GraftDapi dapi,
            WalletPool walletPool)
        {
            _settings = configuration
               .GetSection("ExchangeService")
               .Get<ExchangeServiceConfiguration>();

            _logger = loggerFactory.CreateLogger(nameof(ExchangeService));
            _db = db;
            _rateCache = rateCache;
            _cache = cache;
            _cryptoProviderService = cryptoProviderService;
            _dapi = dapi;
            _walletPool = walletPool;
        }

        // convert cryptocurrency payment to GRFT
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

            if (!((model.BuyCurrency == "GRFT" && model.SellCurrency == "BTC") ||
                (model.BuyCurrency == "GRFT" && model.SellCurrency == "ETH") ||
                (model.BuyCurrency == "USDT" && model.SellCurrency == "GRFT")))
                throw new ApiException(ErrorCode.CurrencyPairNotSupported, $"{model.SellCurrency}->{model.BuyCurrency}");

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

            if (exchange.OutTxId == null)
            {
                if (await _cryptoProviderService.CheckExchange(exchange).ConfigureAwait(false))
                {
                    if (exchange.Status == PaymentStatus.Received)
                    {
                        exchange.Log($"Received Payment of {exchange.ReceivedAmount} {exchange.SellCurrency}");

                        // received amount can be different from initial amount, 
                        // so we need to recalculate all amounts
                        ExchangeCalculator.Recalc(exchange, _settings);

                        await PayToServiceProvider(exchange);
                    }

                    _db.Exchange.Update(exchange);
                    await _db.SaveChangesAsync();
                }
            }

            var res = GetExchangeResult(exchange);
            _logger.LogInformation("ExchangeStatus Result: {@params}", res);
            return res;
        }

        async Task PayToServiceProvider(Exchange exchange)
        {
            if (exchange.OutTxId != null)
                throw new ApiException(ErrorCode.PaymentAlreadyMade);
            exchange.OutTxId = Guid.NewGuid().ToString();

            try
            {
                var wallet = _walletPool.GetPayWallet(exchange.BuyAmount);


                // sale -----------------------------------------
                var dapiParams = new DapiSaleParams
                {
                    PaymentId = exchange.OutTxId,
                    SaleDetails = "sale details string",
                    Address = exchange.BuyerWallet,
                    Amount = GraftConvert.ToAtomicUnits(exchange.BuyAmount)
                };
                var saleResult = await _dapi.Sale(dapiParams).ConfigureAwait(false);


                // sale_status -----------------------------------------
                var dapiStatusParams = new DapiSaleStatusParams
                {
                    PaymentId = exchange.OutTxId,
                    BlockNumber = saleResult.BlockNumber
                };
                var saleStatusResult = await _dapi.GetSaleStatus(dapiStatusParams);


                // sale_details -----------------------------------------
                var dapiSaleDetailsParams = new DapiSaleDetailsParams
                {
                    PaymentId = exchange.OutTxId,
                    BlockNumber = saleResult.BlockNumber
                };
                var saleDetailsResult = await _dapi.SaleDetails(dapiSaleDetailsParams);


                // prepare payment
                var destinations = new List<Destination>();

                // add fee for each node in the AuthSample
                ulong totalAuthSampleFee = 0;
                foreach (var item in saleDetailsResult.AuthSample)
                {
                    destinations.Add(new Destination { Amount = item.Fee, Address = item.Address });
                    totalAuthSampleFee += item.Fee;
                }

                // destination - ServiceProvider
                destinations.Add(new Destination
                {
                    Amount = dapiParams.Amount - totalAuthSampleFee,
                    Address = dapiParams.Address
                });

                var transferParams = new TransferParams
                {
                    Destinations = destinations.ToArray(),
                    DoNotRelay = true,
                    GetTxHex = true,
                    GetTxMetadata = true,
                    GetTxKey = true
                };

                var transferResult = await wallet.TransferRta(transferParams);

                // DAPI pay
                var payParams = new DapiPayParams
                {
                    Address = dapiParams.Address,
                    PaymentId = dapiParams.PaymentId,
                    BlockNumber = saleResult.BlockNumber,
                    Amount = dapiParams.Amount,
                    Transactions = new string[] { transferResult.TxBlob }
                };

                var payResult = await _dapi.Pay(payParams);

                saleStatusResult = await _dapi.GetSaleStatus(dapiStatusParams);
                while ((int)saleStatusResult.Status < (int)DapiSaleStatus.Success)
                {
                    saleStatusResult = await _dapi.GetSaleStatus(dapiStatusParams);
                    await Task.Delay(1000);
                }

                exchange.OutBlockNumber = saleResult.BlockNumber;
                exchange.OutTxStatus = GraftDapi.DapiStatusToPaymentStatus(saleStatusResult.Status);
            }
            catch (Exception ex)
            {
                exchange.OutTxStatus = PaymentStatus.Fail;
                exchange.OutTxStatusDescription = ex.Message;
            }
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
