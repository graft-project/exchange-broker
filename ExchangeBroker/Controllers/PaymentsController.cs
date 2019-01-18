using ExchangeBroker.Data;
using ExchangeBroker.Models.PaymentViewModels;
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
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public PaymentsController(ApplicationDbContext context)
        {
            _db = context;
        }

        public async Task<IActionResult> Index(string filter,
           PaymentStatus? status, GraftTransactionStatus? merchantTranStatus, GraftTransactionStatus? providerTranStatus,
           DateTime? fromDate, DateTime? toDate,
           int page = 1, string sortExpression = "-CreatedAt")
        {
            var query = _db.Payment
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PaymentViewModel
                {
                    PaymentId = p.PaymentId,
                    CreatedAt = p.CreatedAt,
                    Status = p.Status,
                    SaleAmount = p.SaleAmount,
                    SaleCurrency = p.SaleCurrency,
                    PayToSaleRate = p.PayToSaleRate,
                    GraftToSaleRate = p.GraftToSaleRate,
                    PayCurrency = p.PayCurrency,
                    PayAmount = p.PayAmount,
                    ServiceProviderFee = p.ServiceProviderFee,
                    ExchangeBrokerFee = p.ExchangeBrokerFee,
                    MerchantAmount = p.MerchantAmount,
                    PayWalletAddress = p.PayWalletAddress.EllipsisString(5, 5),
                    PayAddressIndex = p.PayAddressIndex,
                    ReceivedConfirmations = p.ReceivedConfirmations,
                    ReceivedAmount = p.ReceivedAmount,
                    ServiceProviderWallet = p.ServiceProviderWallet.EllipsisString(5, 5),
                    MerchantWallet = p.MerchantWallet.EllipsisString(5, 5),
                    MerchantTransactionStatus = p.MerchantTransactionStatus,
                    ProviderTransactionStatus = p.ProviderTransactionStatus
                })
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
                query = query.Where(p => p.PayWalletAddress.Contains(filter) || 
                                p.ServiceProviderWallet.Contains(filter) ||
                                p.MerchantWallet.Contains(filter));

            if (status != null)
                query = query.Where(p => p.Status == status);

            if (merchantTranStatus != null)
                query = query.Where(p => p.MerchantTransactionStatus == merchantTranStatus);

            if (providerTranStatus != null)
                query = query.Where(p => p.ProviderTransactionStatus == providerTranStatus);

            if (fromDate != null)
                query = query.Where(p => p.CreatedAt >= fromDate);

            if (toDate != null)
                query = query.Where(p => p.CreatedAt <= toDate);

            var model = await PagingList.CreateAsync(query, AppConstant.PageSize, page, sortExpression, "-CreatedAt");

            model.RouteValue = new RouteValueDictionary
            {
                { "filter", filter},
                { "status", status },
                { "merchantTranStatus", merchantTranStatus },
                { "providerTranStatus", providerTranStatus },
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

            var payment = await _db.Payment
                .FirstOrDefaultAsync(m => m.PaymentId == id);
            if (payment == null)
            {
                return NotFound();
            }

            return View(new PaymentViewModel
            {
                PaymentId = payment.PaymentId,
                CreatedAt = payment.CreatedAt,
                Status = payment.Status,
                SaleAmount = payment.SaleAmount,
                SaleCurrency = payment.SaleCurrency,
                PayToSaleRate = payment.PayToSaleRate,
                GraftToSaleRate = payment.GraftToSaleRate,
                PayCurrency = payment.PayCurrency,
                PayAmount = payment.PayAmount,
                ServiceProviderFee = payment.ServiceProviderFee,
                ExchangeBrokerFee = payment.ExchangeBrokerFee,
                MerchantAmount = payment.MerchantAmount,
                PayWalletAddress = payment.PayWalletAddress,
                PayAddressIndex = payment.PayAddressIndex,
                ReceivedConfirmations = payment.ReceivedConfirmations,
                ReceivedAmount = payment.ReceivedAmount,
                ServiceProviderWallet = payment.ServiceProviderWallet,
                MerchantWallet = payment.MerchantWallet,
                MerchantTransactionStatus = payment.MerchantTransactionStatus,
                ProviderTransactionStatus = payment.ProviderTransactionStatus
            });
        }

        private bool PaymentExists(string id)
        {
            return _db.Payment.Any(e => e.PaymentId == id);
        }
    }
}
