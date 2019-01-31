using ExchangeBroker.Data;
using Graft.Infrastructure;
using GraftLib;
using GraftLib.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExchangeBroker.Services
{
    public class DatabaseWorker : IDatabaseWorker
    {
        private readonly ApplicationDbContext _db;
        private readonly ApplicationDbContext _dbTransactions;
        private readonly ILogger _logger;

        public DatabaseWorker(string dbConnectionString, ILoggerFactory loggerFactory)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>();
            options.UseMySql(dbConnectionString);

            _db = new ApplicationDbContext(options.Options);
            _dbTransactions = new ApplicationDbContext(options.Options);

            _logger = loggerFactory.CreateLogger<DatabaseWorker>();
        }

        public async Task<IEnumerable<TransactionRequest>> GetNewTransactions()
        {
            IEnumerable<TransactionRequest> result = null;

            try
            {
                var data = _dbTransactions
                    .TransactionRequests
                    .Where(x => x.Status == TransactionRequestStatus.New || x.Status == TransactionRequestStatus.Failed || (x.Status == TransactionRequestStatus.InProgress && (DateTime.Now - x.LastUpdatedTime).TotalMinutes > 20) )
                    .ToList();

                data.ForEach(x => x.Status = TransactionRequestStatus.InProgress);
                await _dbTransactions.SaveChangesAsync();

                result = data;

                _logger.LogInformation($"Found {data.Count} new transactions.");
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Error finding new transactions.");
            }

            return result;
        }

        public async Task<IEnumerable<TransactionRequest>> GetSentTransactions()
        {
            IEnumerable<TransactionRequest> data = new List<TransactionRequest>();

            try
            {
                data = await _db.TransactionRequests
                    .Where(x => x.Status == TransactionRequestStatus.Sent || x.Status == TransactionRequestStatus.RpcFailed)
                    .ToListAsync();

                _logger.LogInformation($"Found {data.Count()} sent transactions.");
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Error finding sent transactions.");
            }

            return data;
        }

        public async Task SetTransactionStatus(IEnumerable<string> ids, bool isFailed, string TxId)
        {
            _logger.LogInformation($"SetTransactionStatus : Setting status {isFailed} with TXID : '{TxId}' for : {string.Join(",", ids)}");
            try
            {
                var transactions = from transaction in _dbTransactions.TransactionRequests
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

                await _dbTransactions.SaveChangesAsync();

                await UpdatePaymentAndExchangeStatuses(isFailed ? TransactionRequestStatus.Failed : TransactionRequestStatus.Sent, ids);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Error SetTransactionStatus.");
            }
        }

        public async Task UpdateTransactionStatus(string id, TransactionRequestStatus transactionRequestStatus)
        {
            _logger.LogInformation($"UpdateTransactionStatus : Setting status {transactionRequestStatus} for : {id}");

            try
            {
                var transaction = await _db.TransactionRequests.SingleOrDefaultAsync(x => x.Id == id);

                transaction.Status = transactionRequestStatus;

                await _db.SaveChangesAsync();
                await UpdatePaymentAndExchangeStatuses(transactionRequestStatus, id);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Error UpdateTransactionStatus.");
            }
        }

        private Task UpdatePaymentAndExchangeStatuses(TransactionRequestStatus transactionRequestStatus, IEnumerable<string> ids)
        {
            return UpdatePaymentAndExchangeStatuses(transactionRequestStatus, ids.ToArray());
        }

        private async Task UpdatePaymentAndExchangeStatuses(TransactionRequestStatus transactionRequestStatus, params string[] ids)
        {
            _logger.LogInformation($"UpdatePaymentAndExchangeStatuses : Setting status {transactionRequestStatus} for : {string.Join(",", ids)}");

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
                var exchanges = from exchange in _db.Exchange
                                where ids.Contains(exchange.BuyerTransactionId)
                                select exchange;

                exchanges.ToList().ForEach(x => x.BuyerTransactionStatus = newStatus);

                await _db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Error UpdatePaymentAndExchangeStatuses : Set Exchanges.");
            }

            try
            {
                var payments = from exchange in _db.Payment
                                where ids.Contains(exchange.MerchantTransactionId)
                                select exchange;

                payments.ToList().ForEach(x => x.MerchantTransactionStatus = newStatus);

                await _db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Error UpdatePaymentAndExchangeStatuses : Set Payments : Merchant transaction.");
            }

            try
            {
                var payments = from exchange in _db.Payment
                               where ids.Contains(exchange.ProviderTransactionId)
                                select exchange;

                payments.ToList().ForEach(x => x.ProviderTransactionStatus = newStatus);

                await _db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Error UpdatePaymentAndExchangeStatuses : Set Payments : Provider transaction.");
            }
        }
    }
}
