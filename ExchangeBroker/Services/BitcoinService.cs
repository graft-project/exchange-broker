using BitcoinLib;
using BitcoinLib.Transactions;
using ExchangeBroker.Data;
using ExchangeBroker.Models;
using ExchangeBroker.Models.Options;
using Graft.Infrastructure;
using Graft.Infrastructure.Watcher;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace ExchangeBroker.Services
{
    public class BitcoinService : WatchableService, IBitcoinService
    {
        private static decimal totalAmountPaymentBySession = 0;
        private static decimal totalAmountExchangeBySession = 0;

        private static long totalPaymentsBySession = 0;
        private static long totalExchangesBySession = 0;

        static ILogger logger;
        static ApplicationDbContext db;
        static CheckTransactionService checker;
        static BitcoinWallet bitcoinWallet;
        static int index = 1;
        static int tryCount;
        static int delayMs;
        static string bitcoinExtPubKeyString;
        static bool isTestNetwork;

        internal static void Init(Action<BitcoinServiceOptions> setupAction)
        {
            var options = new BitcoinServiceOptions();
            setupAction.Invoke(options);

            tryCount = options.TryCount;
            delayMs = options.DelayMs;
            db = options.DbContext;
            bitcoinExtPubKeyString = options.BitcoinExtPubKeyString;
            isTestNetwork = options.IsTestNetwork;

            logger = options.LoggerFactory?.CreateLogger(nameof(BitcoinService));

            int lastPaymentIndex = 0;
            if (db.Payment.Any())
            {
                lastPaymentIndex = db.Payment
                    .Where(t => t.PayCurrency == "BTC")
                    .Max(t => t.PayAddressIndex);
            }

            int lastExchangeIndex = 0;
            if (db.Exchange.Any())
            {
                lastExchangeIndex = db.Exchange
                    .Where(t => t.SellCurrency == "BTC")
                    .Max(t => t.PayAddressIndex);
            }

            index = Math.Max(lastPaymentIndex, lastExchangeIndex) + 1;

            bitcoinWallet = new BitcoinWallet(bitcoinExtPubKeyString, isTestNetwork);
            checker = new CheckTransactionService(isTestNetwork);

            logger.LogInformation("Init: index - {index}, isTestNetwork - {isTestNetwork}, tryCount - {tryCount}, delayMs - {delayMs}",
                index, isTestNetwork, tryCount, delayMs);
        }

        public BitcoinService(
                ILoggerFactory loggerFactory,
                IEmailSender emailService,
                IConfiguration configuration)
            : base(nameof(BitcoinService), "Bitcoin service", loggerFactory, emailService, configuration)
        {
            SetState(WatchableServiceState.OK, "Service instantiated");
        }

        public Task CreateAddress(Payment payment)
        {
            lock (bitcoinWallet)
            {
                payment.PayWalletAddress = bitcoinWallet.GetNewAddress(index);
                payment.PayAddressIndex = index++;
            }

            Metrics[$"Last payment address"] = payment.PayWalletAddress;

            Metrics[$"Last address index"] = index.ToString();

            return Task.CompletedTask;
        }

        public Task CreateAddress(Exchange exchange)
        {
            lock (bitcoinWallet)
            {
                exchange.PayWalletAddress = bitcoinWallet.GetNewAddress(index);
                exchange.PayAddressIndex = index++;
            }

            Metrics[$"Last exchange address"] = exchange.PayWalletAddress;

            Metrics[$"Last address index"] = index.ToString();

            return Task.CompletedTask;
        }

        public async Task CheckPayment(Payment payment)
        {
            try
            {
                var sw = new Stopwatch();
                sw.Start();

                var status = await checker.GetTransactionStatusesByAddress(payment.PayWalletAddress, tryCount, delayMs);
                if (status == null || status.Status == TransactionStatus.NotFound)
                    return;

                decimal receivedAmount = BitcoinConvert.FromAtomicUnits(status.Amount);

                if (status.Status == TransactionStatus.DoubleSpent)
                    payment.Status = PaymentStatus.DoubleSpend;
                else if ((payment.PayAmount - receivedAmount) * payment.PayToSaleRate > 0.01M)
                    payment.Status = PaymentStatus.NotEnoughAmount;
                else
                    payment.Status = PaymentStatus.Received;

                payment.ReceivedConfirmations = status.Confirmations;
                payment.ReceivedAmount = receivedAmount;

                if (State != WatchableServiceState.OK)
                    SetState(WatchableServiceState.OK);

                sw.Stop();
                UpdateStopwatchMetrics(sw, true);

                totalPaymentsBySession++;
                Metrics[$"Total Payments By Session"] = totalPaymentsBySession.ToString();

                totalAmountPaymentBySession += payment.ReceivedAmount;
                Metrics[$"Total Received Payments By Session"] = totalAmountPaymentBySession.ToString();
            }
            catch (Exception ex)
            {
                SetState(WatchableServiceState.Error, ex.Message);
                throw;
            }
            finally
            {
                LastOperationTime = DateTime.UtcNow;
            }
        }

        public async Task<bool> CheckExchange(Exchange exchange)
        {
            bool changed = false;
            try
            {
                var sw = new Stopwatch();
                sw.Start();

                var status = await checker.GetTransactionStatusesByAddress(exchange.PayWalletAddress, tryCount, delayMs);
                if (status == null || status.Status == TransactionStatus.NotFound)
                    return changed;

                decimal receivedAmount = BitcoinConvert.FromAtomicUnits(status.Amount);

                PaymentStatus newStatus = PaymentStatus.Received;
                if (status.Status == TransactionStatus.DoubleSpent)
                    newStatus = PaymentStatus.DoubleSpend;

                if (exchange.Status != newStatus ||
                    exchange.ReceivedConfirmations != status.Confirmations ||
                    exchange.ReceivedAmount != receivedAmount)
                {
                    exchange.Status = newStatus;
                    exchange.ReceivedConfirmations = status.Confirmations;
                    exchange.ReceivedAmount = receivedAmount;
                    changed = true;
                }

                if (State != WatchableServiceState.OK)
                    SetState(WatchableServiceState.OK);

                sw.Stop();
                UpdateStopwatchMetrics(sw, true);

                totalExchangesBySession++;
                Metrics[$"Total Payments By Session"] = totalExchangesBySession.ToString();

                totalAmountExchangeBySession += exchange.ReceivedAmount;
                Metrics[$"Total Received Exchange By Session"] = totalAmountExchangeBySession.ToString();
            }
            catch (Exception ex)
            {
                SetState(WatchableServiceState.Error, ex.Message);
                throw;
            }
            finally
            {
                LastOperationTime = DateTime.UtcNow;
            }
            return changed;
        }

        public string GetUri(Payment payment)
        {
            return $"bitcoin:{payment.PayWalletAddress}?amount={payment.PayAmount.ToString(CultureInfo.InvariantCulture)}";
        }

        public string GetUri(Exchange exchange)
        {
            return $"bitcoin:{exchange.PayWalletAddress}?amount={exchange.SellAmount.ToString(CultureInfo.InvariantCulture)}";
        }

        public override Task Ping()
        {
            Parameters["Bitcoin wallet extPubKey"] = bitcoinExtPubKeyString;
            Parameters["Try count"] = tryCount.ToString();
            Parameters["Delay in ms"] = delayMs.ToString();
            Parameters["Is Testnet"] = isTestNetwork.ToString();

            SetState(WatchableServiceState.OK, "Init finished");

            return Task.CompletedTask;
        }
    }


}
