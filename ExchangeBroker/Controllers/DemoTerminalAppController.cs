using ExchangeBroker.Services;
using Graft.Infrastructure.Broker;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeBroker.Controllers
{
    public class DemoTerminalAppController : Controller
    {
        readonly ILogger _logger;
        readonly IExchangeService _exchangeService;

        public DemoTerminalAppController(ILoggerFactory loggerFactory,
            IExchangeService exchangeService)
        {
            _logger = loggerFactory.CreateLogger(nameof(DemoTerminalAppController));
            _exchangeService = exchangeService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Splash()
        {
            return View();
        }

        public IActionResult SelectCurrency()
        {
            return View();
        }

        public IActionResult SelectCupOfCoffe(string currencyType)
        {
            ViewData["currencyType"] = currencyType;
            return View();
        }

        public IActionResult SelectGraftWallet(string price, string currencyType)
        {
            if (decimal.TryParse(price, out decimal result))
            {
                ViewData["currencyType"] = currencyType;
                ViewData["price"] = price;
                return View();
            }

            return View(nameof(SelectCupOfCoffe));
        }

        public async Task<IActionResult> QrCode(string price, string wallet, string currencyType)
        {
            if (decimal.TryParse(price, out decimal result) && !string.IsNullOrWhiteSpace(wallet) && wallet.Length >= 95)
            {
                BrokerExchangeParams brokerExchangeParams = new BrokerExchangeParams
                {
                    SellFiatAmount = result,
                    WalletAddress = wallet,
                    FiatCurrency = "USD",
                    BuyCurrency = "GRFT",
                    SellCurrency = currencyType.ToUpper()
                };

                BrokerExchangeResult exchangeResult = null;
                try
                {
                    exchangeResult = await CreateExchange(brokerExchangeParams);
                }
                catch (Exception ex)
                {
                    ViewData["ErrorMessage"] = $"{ex.Message} ({ex.InnerException?.Message})";
                    return View("Error");
                }

                ViewData["currencyName"] = currencyType.ToUpper() == "BTC" ? "Bitcoin" : "Ethereum";

                return View(new Tuple<BrokerExchangeResult, string>(exchangeResult, price));
            }

            ViewData["price"] = price;
            return View(nameof(SelectGraftWallet));
        }

        public IActionResult Success()
        {
            return View();
        }

        public Task<IActionResult> CheckExchangeStatus(BrokerExchangeResult checkTransactionModel)
        {
            return Task.FromResult((IActionResult)View(checkTransactionModel));
        }

        public async Task<IActionResult> GetExchangeStatus(string exchangeId)
        {
            var res = await _exchangeService.ExchangeStatus(exchangeId);
            return Json((int)res.Status);
        }

        public async Task<IActionResult> GetExchangeStatusMessage(string exchangeId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(exchangeId))
                {
                    //Here we get statuses before getting exchange id
                    return Json("Initialized!");
                }
                else
                {
                    var res = await _exchangeService.ExchangeStatus(exchangeId);

                    var sb = new StringBuilder();

                    if (res != null && res.ProcessingEvents != null)
                    {
                        foreach (var item in res.ProcessingEvents)
                        {
                            sb.AppendLine($"{item.Time} : {item.Message}");
                        }
                    }
                    //Here we get statuses after getting exchange id
                    return Json(sb.ToString());
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to fetch status");
            }

            return Json("Initialized!");
        }

        public async Task<IActionResult> StatusError(string exchangeId = null)
        {
            var res = await _exchangeService.ExchangeStatus(exchangeId);
            if (res != null)
            {
                ViewData["InTxStatus"] = $"{res.InTxStatus}";
                ViewData["InTxStatusDescription"] = $"{res.InTxStatusDescription}";
                ViewData["OutTxStatus"] = $"{res.OutTxStatus}";
                ViewData["OutTxStatusDescription"] = $"{res.OutTxStatusDescription}";
            }
            else
            {
                ViewData["InTxStatus"] = $"Exchange not found";
            }
            return View();
        }

        internal async Task<BrokerExchangeResult> CreateExchange(BrokerExchangeParams model)
        {
            var exchange = await _exchangeService.Exchange(model);
            return exchange;
        }
    }
}
