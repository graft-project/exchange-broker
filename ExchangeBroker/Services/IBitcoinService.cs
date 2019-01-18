using System.Threading.Tasks;
using ExchangeBroker.Models;
using Graft.Infrastructure.Watcher;

namespace ExchangeBroker.Services
{
    public interface IBitcoinService : ICryptoService, IWatchableService
    {

    }
}