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
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Util;
using Nethereum.Web3.Accounts;
using System;
using System.Numerics;
using System.Threading.Tasks;
using WalletRpc;

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
        readonly GraftDapi _dapi;
        readonly WalletPool _walletPool;
        readonly ExchangeServiceConfiguration _settings;

        public ExchangeToStableService(ILoggerFactory loggerFactory,
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

        public async Task<BrokerExchangeResult> Exchange(BrokerExchangeToStableParams model)
        {
            _logger.LogInformation("Exchange: {@params}", model);

            //ValidateExchangeParams(model);

            // calculate exchange
            Exchange exchange = await ExchangeToStableCalculator.Create(model, _rateCache, _settings);

            // create new sale via DAPI
            var dapiParams = new DapiSaleParams
            {
                PaymentId = Guid.NewGuid().ToString(),
                Address = _settings.IncomeGraftWalletAddress,
                Amount = GraftConvert.ToAtomicUnits(exchange.SellAmount)
            };
            var saleResult = await _dapi.Sale(dapiParams);

            // save payment params
            exchange.InTxId = saleResult.PaymentId;
            exchange.InBlockNumber = saleResult.BlockNumber;

            exchange.PayWalletAddress = _settings.IncomeGraftWalletAddress;
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

            if (string.IsNullOrEmpty(exchangeId))
                throw new ApiException(ErrorCode.ExchangeIdEmpty);

            _cache.TryGetValue(exchangeId, out Exchange exchange);
            if (exchange == null)
            {
                exchange = await _db.Exchange.FirstOrDefaultAsync(t => t.ExchangeId == exchangeId);
                if (exchange == null)
                    throw new ApiException(ErrorCode.ExchangeNotFoundOrExpired);
            }

            // sale_status -----------------------------------------
            var dapiStatusParams = new DapiSaleStatusParams
            {
                PaymentId = exchange.InTxId,
                BlockNumber = exchange.InBlockNumber
            };
            int count = 10;
            var saleStatusResult = await _dapi.GetSaleStatus(dapiStatusParams);
            while (saleStatusResult.Status < DapiSaleStatus.Success)
            {
                saleStatusResult = await _dapi.GetSaleStatus(dapiStatusParams);
                if (count-- < 0)
                    break;
                await Task.Delay(1000);
            }

            if (saleStatusResult.Status == DapiSaleStatus.Success && exchange.OutTxId == null)
            {
                await PayToServiceProvider(exchange);
            }
            else
            {
                exchange.Status = GraftDapi.DapiStatusToPaymentStatus(saleStatusResult.Status);
            }

            _db.Exchange.Update(exchange);
            await _db.SaveChangesAsync();

            var res = GetExchangeResult(exchange);
            _logger.LogInformation("ExchangeStatus Result: {@params}", res);
            return res;
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

                exchange.Status = PaymentStatus.Received;
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
                Status = exchange.Status,

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
