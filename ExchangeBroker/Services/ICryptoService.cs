using ExchangeBroker.Models;
using System.Threading.Tasks;

namespace ExchangeBroker.Services
{
    public interface ICryptoService
    {
        Task CreateAddress(Exchange exchange);
        string GetUri(Exchange exchange);
        Task<bool> CheckExchange(Exchange exchange);
    }
}
