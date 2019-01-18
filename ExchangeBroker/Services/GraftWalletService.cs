using ExchangeBroker.Data;
using ExchangeBroker.Extensions;
using ExchangeBroker.Models;
using Graft.Infrastructure;
using Graft.Infrastructure.Watcher;
using GraftLib;
using GraftLib.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ExchangeBroker.Services
{
    public class GraftWalletService : WatchableService, IGraftWalletService
    {
        static GraftLibService libService;
        private static int totalExchangesBySession;
        private static decimal totalAmountExchangeBySession;
        private static int totalPaymentsBySession;
        private static decimal totalAmountPaymentBySession;

        internal static void Init(ApplicationDbContext db, string url, string DBConnectionString, ILoggerFactory loggerFactory)
        {
            //todo - load data from the DB (where status != TransactionStatusEnum.Out)

            libService = new GraftLibService(new GraftServiceConfiguration()
            {
                ServerUrl = url
            },
            new DatabaseWorker(DBConnectionString, loggerFactory),
            loggerFactory);
        }

        public GraftWalletService(
                ILoggerFactory loggerFactory,
                IEmailSender emailService,
                IConfiguration configuration)
            : base(nameof(GraftWalletService), "Graft Wallet Service", loggerFactory, emailService, configuration)
        {
        }

        public async Task ProcessExchange(Exchange exchange, ApplicationDbContext db)
        {
            try
            {
                var sw = new Stopwatch();
                sw.Start();

                if (string.IsNullOrEmpty(exchange.BuyerTransactionId))
                {
                    exchange.Log($"Initiated payout GRAFT transaction of {exchange.BuyAmount}");

                    var transaction = new TransactionRequest
                    {
                        Address = exchange.BuyerWallet,
                        Amount = GraftConvert.ToAtomicUnits(exchange.BuyAmount)
                    };

                    db.TransactionRequests.Add(transaction);

                    await db.SaveChangesAsync();

                    exchange.BuyerTransactionId = transaction.Id;
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
        }

        public async Task ProcessPayment(Payment payment, ApplicationDbContext db)
        {
            try
            {
                var sw = new Stopwatch();
                sw.Start();

                if (string.IsNullOrEmpty(payment.MerchantTransactionId))
                {
                    var transaction = new TransactionRequest
                    {
                        Address = payment.MerchantWallet,
                        Amount = GraftConvert.ToAtomicUnits(payment.MerchantAmount)
                    };

                    db.TransactionRequests.Add(transaction);

                    await db.SaveChangesAsync();

                    payment.MerchantTransactionId = transaction.Id;
                }

                if (string.IsNullOrEmpty(payment.ProviderTransactionId))
                {
                    var transaction = new TransactionRequest
                    {
                        Address = payment.ServiceProviderWallet,
                        Amount = GraftConvert.ToAtomicUnits(payment.ServiceProviderFee)
                    };

                    db.TransactionRequests.Add(transaction);

                    await db.SaveChangesAsync();

                    payment.ProviderTransactionId = transaction.Id;
                }
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

        public override Task Ping()
        {
            return Task.CompletedTask;
        }
    }
}
