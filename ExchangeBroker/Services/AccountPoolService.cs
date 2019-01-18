using ExchangeBroker.Data;
using Graft.Infrastructure.AccountPool;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExchangeBroker.Services
{
    public class AccountPoolService : IAccountPool
    {
        private readonly ApplicationDbContext applicationDbContext;
        private readonly ILogger logger;

        public AccountPoolService(string dbConnectionString, ILoggerFactory loggerFactory)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>();
            options.UseMySql(dbConnectionString);
            this.applicationDbContext = new ApplicationDbContext(options.Options);
            this.logger = loggerFactory.CreateLogger<AccountPoolService>();
        }

        public async Task<IEnumerable<AccountPoolItem>> GetInactiveAccount(string currencyType)
        {
            IEnumerable<AccountPoolItem> result = new List<AccountPoolItem>();

            try
            {
                result = await applicationDbContext
                .AccountPools
                .Where(x => x.IsProcessed == false && x.CurrencyName == currencyType)
                .ToListAsync();

                logger.LogInformation($"Found {result.Count()} inactive accounts.");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Error finding Inactive Accounts.");
            }

            return result;
        }

        public async Task SetActive(string hash)
        {
            try
            {
                var item = await applicationDbContext.AccountPools.FirstOrDefaultAsync(x => x.Address == hash);

                if (item != null)
                {
                    item.IsProcessed = true;

                    await applicationDbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Error set acc {hash} active.");
                throw;
            }
        }

        public Task SetActive(AccountPoolItem accountPoolItem)
        {
            return SetActive(accountPoolItem.Address);
        }

        public async Task<AccountPoolItem> SetInactive(string hash, string transactionHash, decimal addedValue)
        {
            AccountPoolItem item = null;

            try
            {
                item = await applicationDbContext.AccountPools.FirstOrDefaultAsync(x => x.Address == hash);

                if (item != null)
                {
                    item.IsProcessed = false;

                    item.LastTransactionHash = transactionHash;

                    item.Balance += addedValue;

                    await applicationDbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Error set acc {hash} active incative.");
                throw;
            }

            return null;
        }

        public Task<AccountPoolItem> SetInactive(AccountPoolItem accountPoolItem, string transactionHash)
        {
            return SetInactive(accountPoolItem.Address, transactionHash, accountPoolItem.Balance);
        }

        public async Task ClearBalance(string hash)
        {
            try
            {
                var item = await applicationDbContext.AccountPools.FirstOrDefaultAsync(x => x.Address == hash);

                if (item != null)
                {
                    item.IsProcessed = false;

                    item.Balance = 0;

                    await applicationDbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Error updating balance.");
                throw;
            }
        }

        public async Task WriteNewAccount(AccountPoolItem accountPoolItem)
        {
            try
            {
                applicationDbContext.AccountPools.Add(accountPoolItem);
                await applicationDbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Error creating new acc.");
                throw;
            }
        }

        public async Task<string> GetLastTransactionHash(string accountHash)
        {
            try
            {
                var item = await applicationDbContext.AccountPools.FirstOrDefaultAsync(x => x.Address == accountHash);

                if (item != null)
                {
                    return item.LastTransactionHash;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Error finding Last Transaction Hash.");
                throw;
            }
            return string.Empty;
        }
    }
}
