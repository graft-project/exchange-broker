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
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Util;
using Nethereum.Web3.Accounts;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace ExchangeBroker.Services
{
    [Function("transfer", "bool")]
    public class TransferFunction : FunctionMessage
    {
        [Parameter("address", "_to", 1)]
        public string To { get; set; }

        [Parameter("uint256", "_value", 2)]
        public BigInteger TokenAmount { get; set; }
    }

    public class ExchangeToStableService : IExchangeToStableService
    {
        readonly ILogger _logger;
        readonly ApplicationDbContext _db;
        readonly IRateCache _rateCache;
        readonly IMemoryCache _cache;
        readonly ICryptoProviderService _cryptoProviderService;
        readonly ExchangeServiceConfiguration _settings;
        readonly GraftService _graft;

        public ExchangeToStableService(ILoggerFactory loggerFactory,
            ApplicationDbContext db,
            IConfiguration configuration,
            IRateCache rateCache,
            IMemoryCache cache,
            ICryptoProviderService cryptoProviderService,
            GraftService graft)
        {
            _settings = configuration
               .GetSection("ExchangeService")
               .Get<ExchangeServiceConfiguration>();

            _logger = loggerFactory.CreateLogger(nameof(ExchangeService));
            _db = db;
            _rateCache = rateCache;
            _cache = cache;
            _cryptoProviderService = cryptoProviderService;
            _graft = graft;
        }

        public async Task<BrokerExchangeResult> Exchange(BrokerExchangeToStableParams model)
        {
            _logger.LogInformation("Exchange: {@params}", model);

            //ValidateExchangeParams(model);

            // calculate exchange
            Exchange exchange = await ExchangeToStableCalculator.Create(model, _rateCache, _settings);

            exchange.InTxId = Guid.NewGuid().ToString();
            exchange.InBlockNumber = await _graft.Sale(exchange.InTxId, exchange.SellAmount);

            exchange.PayWalletAddress = _settings.GraftWalletAddress;
            exchange.Log($"{model.SellCurrency} address: {exchange.PayWalletAddress}");

            _cache.Set(exchange.ExchangeId, exchange, DateTimeOffset.Now.AddMinutes(_settings.PaymentTimeoutMinutes));

            _db.Exchange.Add(exchange);
            await _db.SaveChangesAsync();

            var res = GetExchangeResult(exchange);
            _logger.LogInformation("Exchange Result: {@params}", res);
            return res;
        }

        public async Task<BrokerExchangeResult> ExchangeStatus(string exchangeId)
        {
            _logger.LogInformation("ExchangeStatus: {@params}", exchangeId);

            Exchange exchange = await GetExchange(exchangeId);

            PaymentStatus status = await _graft.GetSaleStatus(exchange.InTxId, exchange.InBlockNumber);

            //// sale_status -----------------------------------------
            //var dapiStatusParams = new DapiSaleStatusParams
            //{
            //    PaymentId = exchange.InTxId,
            //    BlockNumber = exchange.InBlockNumber
            //};
            //int count = 10;
            //var saleStatusResult = await _dapi.GetSaleStatus(dapiStatusParams);
            //while (saleStatusResult.Status < DapiSaleStatus.Success)
            //{
            //    saleStatusResult = await _dapi.GetSaleStatus(dapiStatusParams);
            //    if (count-- < 0)
            //        break;
            //    await Task.Delay(1000);
            //}

            if (status >= PaymentStatus.Received && exchange.OutTxId == null)
            {
                await PayToServiceProvider(exchange);
            }
            //else
            //{
            //    exchange.Status = status;
            //}

            _db.Exchange.Update(exchange);
            await _db.SaveChangesAsync();

            var res = GetExchangeResult(exchange);
            _logger.LogInformation("ExchangeStatus Result: {@params}", res);
            return res;
        }

        async Task<Exchange> GetExchange(string exchangeId)
        {
            if (string.IsNullOrEmpty(exchangeId))
                throw new ApiException(ErrorCode.ExchangeIdEmpty);

            _cache.TryGetValue(exchangeId, out Exchange exchange);
            if (exchange == null)
            {
                exchange = await _db.Exchange.FirstOrDefaultAsync(t => t.ExchangeId == exchangeId);
                if (exchange == null)
                    throw new ApiException(ErrorCode.ExchangeNotFoundOrExpired);
            }

            return exchange;
        }

        async Task PayToServiceProvider(Exchange exchange)
        {
            if (exchange.OutTxId != null)
                throw new ApiException(ErrorCode.PaymentAlreadyMade);
            exchange.OutTxId = string.Empty;

            try
            {
                var web3 = new Nethereum.Web3.Web3(new Account(_settings.EthereumPrivatekey), _settings.EthereumUrl);

                var transactionMessage = new TransferFunction()
                {
                    FromAddress = _settings.EthereumAddress,
                    To = exchange.BuyerWallet,// receiverAddress,
                    TokenAmount = (BigInteger)exchange.BuyAmount,
                    GasPrice = Nethereum.Web3.Web3.Convert.ToWei(25, UnitConversion.EthUnit.Gwei)
                };

                var transferHandler = web3.Eth.GetContractTransactionHandler<TransferFunction>();

                var estimate = await transferHandler.EstimateGasAsync(_settings.StableCoinContractAddress, transactionMessage);
                transactionMessage.Gas = estimate.Value;

                var transactionHash = await transferHandler.SendRequestAsync(_settings.StableCoinContractAddress, transactionMessage);

                //exchange.Status = PaymentStatus.Received;
                exchange.OutTxStatus = PaymentStatus.Confirmed;
                exchange.OutTxId = transactionHash;
            }
            catch (Exception ex)
            {
                exchange.OutTxId = null;
                exchange.OutTxStatus = PaymentStatus.Fail;
                exchange.OutTxStatusDescription = ex.Message;
            }
        }

        BrokerExchangeResult GetExchangeResult(Exchange exchange)
        {
            return new BrokerExchangeResult()
            {
                ExchangeId = exchange.ExchangeId,
                Status = (PaymentStatus)Math.Min((sbyte)exchange.InTxStatus, (sbyte)exchange.OutTxStatus),
                //Status = exchange.ExchangeStatus,

                SellCurrency = exchange.SellCurrency,
                SellAmount = exchange.SellAmount,

                BuyCurrency = exchange.BuyCurrency,
                BuyAmount = exchange.BuyAmount,

                SellToUsdRate = exchange.SellToUsdRate,
                GraftToUsdRate = exchange.GraftToUsdRate,

                ExchangeBrokerFeeRate = _settings.ExchangeBrokerFee,
                ExchangeBrokerFeeAmount = exchange.ExchangeBrokerFee,

                PayWalletAddress = exchange.PayWalletAddress,
                GraftPaymentId = exchange.InTxId,
                GraftBlockNumber = exchange.InBlockNumber,

                ProcessingEvents = exchange.ProcessingEvents
            };
        }
    }
}
