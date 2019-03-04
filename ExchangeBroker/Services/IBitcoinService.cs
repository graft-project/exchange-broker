using Graft.Infrastructure.Watcher;

namespace ExchangeBroker.Services
{
    public interface IBitcoinService : ICryptoService, IWatchableService
    {
    }
}