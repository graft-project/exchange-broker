using ExchangeBroker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
        Task CheckExchange(Exchange exchange);
    }
}
