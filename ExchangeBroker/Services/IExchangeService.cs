using Graft.Infrastructure.Broker;
using System.Threading.Tasks;

namespace ExchangeBroker.Services
{
    public interface IExchangeService
    {
        Task<BrokerExchangeResult> Exchange(BrokerExchangeParams model);
        Task<BrokerExchangeResult> ExchangeStatus(string exchangeId);
    }
}