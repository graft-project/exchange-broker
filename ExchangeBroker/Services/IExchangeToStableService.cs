using Graft.Infrastructure.Broker;
using System.Threading.Tasks;

namespace ExchangeBroker.Services
{
    public interface IExchangeToStableService
    {
        Task<BrokerExchangeResult> Exchange(BrokerExchangeToStableParams model);
        Task<BrokerExchangeResult> ExchangeStatus(string exchangeId);
    }
}