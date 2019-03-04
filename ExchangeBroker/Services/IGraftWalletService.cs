using ExchangeBroker.Data;
using ExchangeBroker.Models;
using Graft.Infrastructure.Watcher;
using System.Threading.Tasks;

namespace ExchangeBroker.Services
{
    public interface IGraftWalletService : IWatchableService
    {
        Task ProcessExchange(Exchange exchange, ApplicationDbContext db);
    }
}