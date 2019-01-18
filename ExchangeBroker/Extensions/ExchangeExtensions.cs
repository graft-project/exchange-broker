using ExchangeBroker.Models;
using Graft.Infrastructure;

namespace ExchangeBroker.Extensions
{
    public static class ExchangeExtensions
    {
        public static void Log(this Exchange exchange, string message)
        {
            exchange.ProcessingEvents?.Add(new EventItem(message));
        }
    }
}
