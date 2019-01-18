using ExchangeBroker.Data;
using Microsoft.Extensions.Logging;

namespace ExchangeBroker.Models.Options
{
    public class BitcoinServiceOptions
    {
        public bool IsTestNetwork { get; set; } = true;
        public int TryCount { get; set; } = 3;
        public int DelayMs { get; set; } = 3000;
        public ApplicationDbContext DbContext { get; set; }
        public ILoggerFactory LoggerFactory { get; set; }
        public string BitcoinExtPubKeyString { get; set; }
    }
}
