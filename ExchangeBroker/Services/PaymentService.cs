using ExchangeBroker.Data;
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
    public class PaymentService : IPaymentService
    {
        readonly ILogger _logger;
        readonly ApplicationDbContext _db;
        readonly IRateCache _rateCache;
        readonly IMemoryCache _cache;
        readonly ICryptoProviderService _cryptoProviderService;
        readonly IGraftWalletService _wallet;
        readonly PaymentServiceConfiguration _settings;

        public PaymentService(ILoggerFactory loggerFactory,
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

            _logger = loggerFactory.CreateLogger(nameof(PaymentService));
            _db = db;
            _rateCache = rateCache;
            _cache = cache;
            _cryptoProviderService = cryptoProviderService;
            _wallet = wallet;
        }

        public async Task<BrokerSaleResult> Sale(BrokerSaleParams model)
        {
            _logger.LogInformation("Sale: {@params}", model);

            ValidateSaleParams(model);

            // payment calculation
            var btcRate = await _rateCache.GetRateToUsd(model.PayCurrency);
            var graftRate = await _rateCache.GetRateToUsd("GRFT");

            var btcAmount = model.SaleAmount / btcRate;
            var graftAmount = model.SaleAmount / graftRate;

            var serviceProviderFee = graftAmount * (decimal)model.ServiceProviderFee;
            var exchangeBrokerFee = graftAmount * _settings.ExchangeBrokerFee;

            var merchantAmount = graftAmount - serviceProviderFee - exchangeBrokerFee;

            var payment = new Payment
            {
                PaymentId = model.PaymentId ?? Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow,
                Status = PaymentStatus.Waiting,

                SaleAmount = model.SaleAmount,
                SaleCurrency = model.SaleCurrency,

                PayToSaleRate = btcRate,
                GraftToSaleRate = graftRate,

                PayCurrency = model.PayCurrency,
                PayAmount = btcAmount,

                ServiceProviderFee = serviceProviderFee,
                ExchangeBrokerFee = exchangeBrokerFee,
                MerchantAmount = merchantAmount,

                ServiceProviderWallet = model.ServiceProviderWallet,
                MerchantWallet = model.MerchantWallet,

                MerchantTransactionStatus = GraftTransactionStatus.New,
                ProviderTransactionStatus = GraftTransactionStatus.New
            };

            await _cryptoProviderService.CreateAddress(payment);

            _cache.Set(payment.PaymentId, payment, DateTimeOffset.Now.AddMinutes(_settings.PaymentTimeoutMinutes));

            _db.Payment.Add(payment);
            await _db.SaveChangesAsync();

            var res = GetSaleResult(payment);
            _logger.LogInformation("Sale Result: {@params}", res);
            return res;
        }

        void ValidateSaleParams(BrokerSaleParams model)
        {
            if (model.SaleAmount <= 0)
                throw new ApiException(ErrorCode.InvalidAmount);

            if (string.IsNullOrWhiteSpace(model.SaleCurrency))
                throw new ApiException(ErrorCode.SaleCurrencyEmpty);

            if (model.SaleCurrency != "USD")
                throw new ApiException(ErrorCode.SaleCurrencyNotSupported, model.SaleCurrency);

            if (string.IsNullOrWhiteSpace(model.PayCurrency))
                throw new ApiException(ErrorCode.PayCurrencyEmpty);

            if (!_rateCache.IsSupported(model.PayCurrency))
                throw new ApiException(ErrorCode.PayCurrencyNotSupported, model.PayCurrency);

            if (model.ServiceProviderFee < 0 || model.ServiceProviderFee > _settings.MaxServiceProviderFee)
                throw new ApiException(ErrorCode.InvalidServiceProviderFee);

            if (model.ServiceProviderFee > 0 && string.IsNullOrWhiteSpace(model.ServiceProviderWallet))
                throw new ApiException(ErrorCode.ServiceProviderWalletEmpty);

            if (string.IsNullOrWhiteSpace(model.MerchantWallet))
                throw new ApiException(ErrorCode.MerchantWalletEmpty);
        }

        public async Task<BrokerSaleStatusResult> SaleStatus(BrokerSaleStatusParams model)
        {
            _logger.LogInformation("SaleStatus: {@params}", model);

            ValidateSaleStatusParams(model);

            _cache.TryGetValue(model.PaymentId, out Payment payment);
            if (payment == null)
            {
                payment = await _db.Payment.FirstOrDefaultAsync(t => t.PaymentId == model.PaymentId);
                if (payment == null)
                    throw new ApiException(ErrorCode.PaymentNotFoundOrExpired);
            }

            await _cryptoProviderService.CheckPayment(payment);

            if (payment.Status == PaymentStatus.Received)
            {
                await _wallet.ProcessPayment(payment, _db);
            }


            _db.Payment.Update(payment);
            await _db.SaveChangesAsync();

            var res = GetSaleStatusResult(payment);
            _logger.LogInformation("SaleStatus Result: {@params}", res);
            return res;
        }

        void ValidateSaleStatusParams(BrokerSaleStatusParams model)
        {
            if (string.IsNullOrEmpty(model.PaymentId))
                throw new ApiException(ErrorCode.PaymentIdEmpty);
        }

        BrokerSaleResult GetSaleResult(Payment payment)
        {
            return new BrokerSaleResult()
            {
                PaymentId = payment.PaymentId,

                SaleAmount = payment.SaleAmount,
                SaleCurrency = payment.SaleCurrency,

                PayToSaleRate = payment.PayToSaleRate,
                GraftToSaleRate = payment.GraftToSaleRate,

                PayCurrency = payment.PayCurrency,
                PayAmount = payment.PayAmount,

                ServiceProviderFee = payment.ServiceProviderFee,
                ExchangeBrokerFee = payment.ExchangeBrokerFee,
                MerchantAmount = payment.MerchantAmount,

                PayWalletAddress = _cryptoProviderService.GetUri(payment)
            };
        }

        BrokerSaleStatusResult GetSaleStatusResult(Payment payment)
        {
            return new BrokerSaleStatusResult()
            {
                PaymentId = payment.PaymentId,
                Status = payment.Status
            };
        }

    }
}
