using ExchangeBroker.Data;
using ExchangeBroker.Services;
using Graft.Infrastructure;
using Graft.Infrastructure.Broker;
using Graft.Infrastructure.Models;
using Graft.Infrastructure.Rate;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace ExchangeBroker.Controllers.Api
{
    [Route("v1.0")]
    [ApiController]
    public class ApiPaymentsController : ControllerBase
    {
        readonly ILogger _logger;
        readonly ApplicationDbContext _db;
        readonly IRateCache _rateCache;
        readonly IExchangeService _exchangeService;
        readonly IPaymentService _paymentService;

        public ApiPaymentsController(ILoggerFactory loggerFactory,
            ApplicationDbContext db,
            IRateCache rateCache,
            IExchangeService exchangeService,
            IPaymentService paymentService)
        {
            _logger = loggerFactory.CreateLogger(nameof(ApiPaymentsController));
            _db = db;
            _rateCache = rateCache;
            _exchangeService = exchangeService;
            _paymentService = paymentService;
        }

        [HttpPost]
        [Route("Exchange")]
        public async Task<IActionResult> Exchange([FromBody] BrokerExchangeParams model)
        {
            var res = await _exchangeService.Exchange(model);
            return Ok(res);
        }

        [HttpPost]
        [Route("Sale")]
        public async Task<IActionResult> Sale([FromBody] BrokerSaleParams model)
        {
            var res = await _paymentService.Sale(model);
            return Ok(res);
        }

        [HttpPost]
        [Route("SaleStatus")]
        public async Task<IActionResult> SaleStatus([FromBody] BrokerSaleStatusParams model)
        {
            var res = await _paymentService.SaleStatus(model);
            return Ok(res);
        }

        [HttpPost]
        [Route("ExchangeStatus")]
        public async Task<IActionResult> ExchangeStatus([FromBody] BrokerExchangeStatusParams model)
        {
            var res = await _exchangeService.ExchangeStatus(model.ExchangeId);
            return Ok(res); 
        }

        [HttpGet]
        [Route("GetParams")]
        public IActionResult GetParams()
        {
            var res = new BrokerParams()
            {
                Currencies = new string[] { "USD" },
                Cryptocurrencies = _rateCache.GetSupportedCurrencies()
            };

            _logger.LogInformation("API GetParams: {@params}", res);
            return Ok(res);
        }

        #region Helpers
        IActionResult Error(ErrorCode errorCode, object param = null)
        {
            var error = new ApiError(errorCode, param);
            _logger.LogError("API Error ({code}) {message}", error.Code, error.Message);
            return BadRequest(new ApiErrorResult(error));
        }
        #endregion
    }
}