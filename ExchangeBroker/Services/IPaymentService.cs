using System.Threading.Tasks;
using Graft.Infrastructure.Broker;

namespace ExchangeBroker.Services
{
    public interface IPaymentService
    {
        Task<BrokerSaleResult> Sale(BrokerSaleParams model);
        Task<BrokerSaleStatusResult> SaleStatus(BrokerSaleStatusParams model);
    }
}