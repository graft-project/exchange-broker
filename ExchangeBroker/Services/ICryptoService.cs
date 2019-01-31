using ExchangeBroker.Models;
using System.Threading.Tasks;

namespace ExchangeBroker.Services
{
    public interface ICryptoService
    {
        Task CreateAddress(Payment payment);
        Task CreateAddress(Exchange exchange);

        string GetUri(Payment payment);
        string GetUri(Exchange exchange);

        Task CheckPayment(Payment payment);
        Task<bool> CheckExchange(Exchange exchange);
    }
}
