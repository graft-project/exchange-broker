using ExchangeBroker.Data;
using ExchangeBroker.Models.ExchangeViewModels;
using Graft.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using ReflectionIT.Mvc.Paging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ExchangeBroker.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ExchangesController : Controller
    {
        readonly ApplicationDbContext _db;

        public ExchangesController(ApplicationDbContext context)
        {
            _db = context;
        }

        public async Task<IActionResult> Index(string filter, 
            PaymentStatus? status, GraftTransactionStatus? buyerTranStatus,
            DateTime? fromDate, DateTime? toDate,
            int page = 1, string sortExpression = "-CreatedAt")
        {
            var query = _db.Exchange
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new ExchangeViewModel
                {
                    ExchangeId = p.ExchangeId,
                    CreatedAt = p.CreatedAt,
                    Status = p.Status,
                    SellAmount = p.SellAmount,
                    SellCurrency = p.SellCurrency,
                    BuyAmount = p.BuyAmount,
                    BuyCurrency = p.BuyCurrency,
                    SellToUsdRate = p.SellToUsdRate,
                    GraftToUsdRate = p.GraftToUsdRate,
                    ExchangeBrokerFee = p.ExchangeBrokerFee,
                    BuyerWallet = p.BuyerWallet.EllipsisString(5, 5),
                    PayWalletAddress = p.PayWalletAddress.EllipsisString(5, 5),
                    PayAddressIndex = p.PayAddressIndex,
                    ReceivedConfirmations = p.ReceivedConfirmations,
                    ReceivedAmount = p.ReceivedAmount,
                    BuyerTransactionStatus = p.BuyerTransactionStatus
                })
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
                query = query.Where(p => p.PayWalletAddress.Contains(filter) || p.BuyerWallet.Contains(filter));

            if (status != null)
                query = query.Where(p => p.Status == status);

            if (buyerTranStatus != null)
                query = query.Where(p => p.BuyerTransactionStatus == buyerTranStatus);

            if (fromDate != null)
                query = query.Where(p => p.CreatedAt >= fromDate);

            if (toDate != null)
                query = query.Where(p => p.CreatedAt <= toDate);

            var model = await PagingList.CreateAsync(query, AppConstant.PageSize, page, sortExpression, "-CreatedAt");

            model.RouteValue = new RouteValueDictionary
            {
                { "filter", filter},
                { "status", status },
                { "buyerTranStatus", buyerTranStatus },
                { "from", fromDate },
                { "to", toDate }
            };

            return View(model);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var exchange = await _db.Exchange
                .FirstOrDefaultAsync(m => m.ExchangeId == id);
            if (exchange == null)
            {
                return NotFound();
            }

            return View(new ExchangeViewModel
            {
                ExchangeId = exchange.ExchangeId,
                CreatedAt = exchange.CreatedAt,
                Status = exchange.Status,
                SellAmount = exchange.SellAmount,
                SellCurrency = exchange.SellCurrency,
                BuyAmount = exchange.BuyAmount,
                BuyCurrency = exchange.BuyCurrency,
                SellToUsdRate = exchange.SellToUsdRate,
                GraftToUsdRate = exchange.GraftToUsdRate,
                ExchangeBrokerFee = exchange.ExchangeBrokerFee,
                BuyerWallet = exchange.BuyerWallet,
                PayWalletAddress = exchange.PayWalletAddress,
                PayAddressIndex = exchange.PayAddressIndex,
                ReceivedConfirmations = exchange.ReceivedConfirmations,
                ReceivedAmount = exchange.ReceivedAmount,
                BuyerTransactionStatus = exchange.BuyerTransactionStatus
            });
        }

        private bool ExchangeExists(string id)
        {
            return _db.Exchange.Any(e => e.ExchangeId == id);
        }
    }
}
