using ExchangeBroker.Data;
using ExchangeBroker.Models;
using Graft.Infrastructure.Watcher;
using System.Threading.Tasks;

namespace ExchangeBroker.Services
{
    public interface IGraftWalletService : IWatchableService
    {
        Task ProcessPayment(Payment payment, ApplicationDbContext db);
        Task ProcessExchange(Exchange exchange, ApplicationDbContext db);
    }
}