using ExchangeBroker.Data;
using Graft.Infrastructure;
using Graft.Infrastructure.AccountPool;
using GraftLib;
using GraftLib.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExchangeBroker.Services
{
    public class DatabaseWorker : IDatabaseWorker
    {
        private readonly ApplicationDbContext applicationDbContext;
        private readonly ApplicationDbContext applicationDbContextTransactions;
        private readonly ILogger logger;

        public DatabaseWorker(string dbConnectionString, ILoggerFactory loggerFactory)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>();
            options.UseMySql(dbConnectionString);
            this.applicationDbContext = new ApplicationDbContext(options.Options);
            this.applicationDbContextTransactions = new ApplicationDbContext(options.Options);
            this.logger = loggerFactory.CreateLogger<DatabaseWorker>();
        }

        public async Task<IEnumerable<TransactionRequest>> GetNewTransactions()
        {
            IEnumerable<TransactionRequest> result = null;

            try
            {
                var data = applicationDbContextTransactions
                    .TransactionRequests
                    .Where(x => x.Status == TransactionRequestStatus.New || x.Status == TransactionRequestStatus.Failed || (x.Status == TransactionRequestStatus.InProgress && (DateTime.Now - x.LastUpdatedTime).TotalMinutes > 20) )
                    .ToList();

                data.ForEach(x => x.Status = TransactionRequestStatus.InProgress);
                await applicationDbContextTransactions.SaveChangesAsync();

                result = data;

                logger.LogInformation($"Found {data.Count} new transactions.");
            }
            catch (Exception e)
            {
                logger.LogWarning(e, $"Error finding new transactions.");
            }

            return result;
        }

        public async Task<IEnumerable<TransactionRequest>> GetSentTransactions()
        {
            IEnumerable<TransactionRequest> data = new List<TransactionRequest>();

            try
            {
                data = await applicationDbContext.TransactionRequests.Where(x => x.Status == TransactionRequestStatus.Sent || x.Status == TransactionRequestStatus.RpcFailed).ToListAsync();
                logger.LogInformation($"Found {data.Count()} sent transactions.");
            }
            catch (Exception e)
            {
                logger.LogWarning(e, $"Error finding sent transactions.");
            }

            return data;
        }

        public async Task SetTransactionStatus(IEnumerable<string> ids, bool isFailed, string TxId)
        {
            logger.LogInformation($"SetTransactionStatus : Setting status {isFailed} with TXID : '{TxId}' for : {string.Join(",", ids)}");
            try
            {
                var transactions = from transaction in applicationDbContextTransactions.TransactionRequests
                                   where ids.Contains(transaction.Id)
                                   select transaction;
                if (isFailed)
                {
                    transactions.ToList().ForEach(x => x.Status = TransactionRequestStatus.Failed);
                }
                else
                {
                    transactions.ToList().ForEach(x =>
                    {
                        x.Status = TransactionRequestStatus.Sent;
                        x.TxId = TxId;
                    });
                }

                await applicationDbContextTransactions.SaveChangesAsync();

                await UpdatePaymentAndExchangeStatuses(isFailed ? TransactionRequestStatus.Failed : TransactionRequestStatus.Sent, ids);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, $"Error SetTransactionStatus.");
            }
        }

        public async Task UpdateTransactionStatus(string id, TransactionRequestStatus transactionRequestStatus)
        {
            logger.LogInformation($"UpdateTransactionStatus : Setting status {transactionRequestStatus} for : {id}");

            try
            {
                var transaction = await applicationDbContext.TransactionRequests.SingleOrDefaultAsync(x => x.Id == id);

                transaction.Status = transactionRequestStatus;

                await applicationDbContext.SaveChangesAsync();
                await UpdatePaymentAndExchangeStatuses(transactionRequestStatus, id);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, $"Error UpdateTransactionStatus.");
            }
        }

        private Task UpdatePaymentAndExchangeStatuses(TransactionRequestStatus transactionRequestStatus, IEnumerable<string> ids)
        {
            return UpdatePaymentAndExchangeStatuses(transactionRequestStatus, ids.ToArray());
        }

        private async Task UpdatePaymentAndExchangeStatuses(TransactionRequestStatus transactionRequestStatus, params string[] ids)
        {
            logger.LogInformation($"UpdatePaymentAndExchangeStatuses : Setting status {transactionRequestStatus} for : {string.Join(",", ids)}");

            GraftTransactionStatus newStatus;

            switch (transactionRequestStatus)
            {
                case TransactionRequestStatus.Failed:
                    newStatus = GraftTransactionStatus.Failed;
                    break;
                case TransactionRequestStatus.New:
                    newStatus = GraftTransactionStatus.New;
                    break;
                case TransactionRequestStatus.InProgress:
                case TransactionRequestStatus.Sent:
                    newStatus = GraftTransactionStatus.Pending;
                    break;
                case TransactionRequestStatus.Out:
                    newStatus = GraftTransactionStatus.Out;
                    break;
                default:
                    newStatus = GraftTransactionStatus.NotFound;
                    break;
            }

            try
            {
                var exchanges = from exchange in applicationDbContext.Exchange
                                where ids.Contains(exchange.BuyerTransactionId)
                                select exchange;

                exchanges.ToList().ForEach(x => x.BuyerTransactionStatus = newStatus);

                await applicationDbContext.SaveChangesAsync();
            }
            catch (Exception e)
            {
                logger.LogWarning(e, $"Error UpdatePaymentAndExchangeStatuses : Set Exchanges.");
            }

            try
            {
                var payments = from exchange in applicationDbContext.Payment
                                where ids.Contains(exchange.MerchantTransactionId)
                                select exchange;

                payments.ToList().ForEach(x => x.MerchantTransactionStatus = newStatus);

                await applicationDbContext.SaveChangesAsync();
            }
            catch (Exception e)
            {
                logger.LogWarning(e, $"Error UpdatePaymentAndExchangeStatuses : Set Payments : Merchant transaction.");
            }

            try
            {
                var payments = from exchange in applicationDbContext.Payment
                               where ids.Contains(exchange.ProviderTransactionId)
                                select exchange;

                payments.ToList().ForEach(x => x.ProviderTransactionStatus = newStatus);

                await applicationDbContext.SaveChangesAsync();
            }
            catch (Exception e)
            {
                logger.LogWarning(e, $"Error UpdatePaymentAndExchangeStatuses : Set Payments : Provider transaction.");
            }
        }
    }
}
