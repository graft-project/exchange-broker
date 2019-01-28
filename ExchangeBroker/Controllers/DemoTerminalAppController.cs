using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ExchangeBroker.Data;
using Graft.Infrastructure.Broker;
using ExchangeBroker.Services;
using ExchangeBroker.Controllers.Api;
using Graft.Infrastructure.Rate;
using Microsoft.Extensions.Caching.Memory;
using ExchangeBroker.Models;
using Graft.Infrastructure;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace ExchangeBroker.Controllers
{
    public class DemoTerminalAppController : Controller
    {
        readonly ILogger logger;
        readonly IExchangeService exchangeService;

        public DemoTerminalAppController(ILoggerFactory loggerFactory,
            IExchangeService exchangeService)
        {
            this.logger = loggerFactory.CreateLogger(nameof(DemoTerminalAppController));
            this.exchangeService = exchangeService;
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
            decimal result;
            if (decimal.TryParse(price, out result))
            {
                ViewData["currencyType"] = currencyType;
                ViewData["price"] = price;
                return View();
            }

            return View(nameof(SelectCupOfCoffe));
        }

        public async Task<IActionResult> QrCode(string price, string wallet, string currencyType)
        {
            decimal result;

            if( decimal.TryParse(price, out result) && !string.IsNullOrWhiteSpace(wallet) && wallet.Length >= 95)
            {
                BrokerExchangeParams brokerExchangeParams = new BrokerExchangeParams();

                brokerExchangeParams.SellFiatAmount = result;
                brokerExchangeParams.WalletAddress = wallet;
                brokerExchangeParams.FiatCurrency = "USD";
                brokerExchangeParams.BuyCurrency = "GRFT";
                brokerExchangeParams.SellCurrency = currencyType.ToUpper();

                var exchangeResult = await CreateExchange(brokerExchangeParams);

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
            var res = await exchangeService.ExchangeStatus(exchangeId);
           
            return Json(res.Status.ToString());
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
                    var res = await exchangeService.ExchangeStatus(exchangeId);

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
                logger.LogError(e, "Faild to fetch status");
            }

            return Json("Initialized!");
        }

        internal async Task<BrokerExchangeResult> CreateExchange(BrokerExchangeParams model)
        {
            var exchange = await exchangeService.Exchange(model);
            return exchange;
        }
    }
}
