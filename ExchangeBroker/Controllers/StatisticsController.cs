using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ExchangeBroker.Data;
using ExchangeBroker.Models;
using Microsoft.AspNetCore.Authorization;

namespace ExchangeBroker.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StatisticsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StatisticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string variant = null)
        {
            ISearchModel model;

            if (variant != null && variant == "Payments")
            {
                model = new SearchModelPayments
                {
                    Data = await _context.Payment.ToListAsync()
                };
            }
            else
            {
                model = new SearchModelExchange
                {
                    Data = await _context.Exchange.ToListAsync()
                };
            }

            return View(model);
        }
    }

    public interface ISearchModel
    {
        int TotalCount { get; }

        string ItemTypeTo { get; }

        int GetStatusCount(Graft.Infrastructure.PaymentStatus status);

        int FailedCount
        {
            get;
        }

        int InProgressCount
        {
            get;
        }

        int SuccessfulCount
        {
            get;
        }

        Dictionary<string, int> DataSortedByDays
        {
            get;
        }

        Dictionary<string, int> DataSortedByPayment
        {
            get;
        }
    }

    public class SearchModelExchange : ISearchModel
    {
        public string ItemTypeTo { get => "Exchanges"; }

        public int GetStatusCount(Graft.Infrastructure.PaymentStatus status)
        {
            return Data.Count(x => x.Status == status);
        }

        public int FailedCount
        {
            get => Data.Count(x => new[]
            {
                Graft.Infrastructure.PaymentStatus.TimedOut,
                Graft.Infrastructure.PaymentStatus.RejectedByWallet,
                Graft.Infrastructure.PaymentStatus.RejectedByPOS,
                Graft.Infrastructure.PaymentStatus.NotEnoughAmount,
                Graft.Infrastructure.PaymentStatus.Fail,
                Graft.Infrastructure.PaymentStatus.DoubleSpend
            }.Contains(x.Status));
        }

        public int InProgressCount
        {
            get => Data.Count(x => new[]
            {
                Graft.Infrastructure.PaymentStatus.InProgress,
                Graft.Infrastructure.PaymentStatus.New,
                Graft.Infrastructure.PaymentStatus.Waiting
            }.Contains(x.Status));
        }

        public int SuccessfulCount
        {
            get => Data.Count(x => new[]
            {
                Graft.Infrastructure.PaymentStatus.Received,
                Graft.Infrastructure.PaymentStatus.Confirmed
            }.Contains(x.Status));
        }

        public Dictionary<string, int> DataSortedByDays
        {
            get => Data.GroupBy(x => x.CreatedAt.Date).ToDictionary(x => x.Key.ToShortDateString(), y => y.Count());
        }

        public Dictionary<string, int> DataSortedByPayment
        {
            get => Data.OrderBy(x => x.SellAmount).GroupBy(x => Math.Round(x.SellAmount, 5)).ToDictionary(x => $"{x.Key} BTC", y => y.Count());
        }

        public List<Exchange> Data { get; set; } = new List<Exchange>();

        public int TotalCount => Data.Count;
    }

    public class SearchModelPayments : ISearchModel
    {
        public string ItemTypeTo { get => "Payments"; }

        public int GetStatusCount(Graft.Infrastructure.PaymentStatus status)
        {
            return Data.Count(x => x.Status == status);
        }

        public int FailedCount
        {
            get => Data.Count(x => new[]
            {
                Graft.Infrastructure.PaymentStatus.TimedOut,
                Graft.Infrastructure.PaymentStatus.RejectedByWallet,
                Graft.Infrastructure.PaymentStatus.RejectedByPOS,
                Graft.Infrastructure.PaymentStatus.NotEnoughAmount,
                Graft.Infrastructure.PaymentStatus.Fail,
                Graft.Infrastructure.PaymentStatus.DoubleSpend
            }.Contains(x.Status));
        }

        public int InProgressCount
        {
            get => Data.Count(x => new[]
            {
                Graft.Infrastructure.PaymentStatus.InProgress,
                Graft.Infrastructure.PaymentStatus.New,
                Graft.Infrastructure.PaymentStatus.Waiting
            }.Contains(x.Status));
        }

        public int SuccessfulCount
        {
            get => Data.Count(x => new[]
            {
                Graft.Infrastructure.PaymentStatus.Received,
                Graft.Infrastructure.PaymentStatus.Confirmed
            }.Contains(x.Status));
        }

        public Dictionary<string, int> DataSortedByDays
        {
            get => Data.GroupBy(x => x.CreatedAt.Date).ToDictionary(x => x.Key.ToShortDateString(), y => y.Count());
        }

        public Dictionary<string, int> DataSortedByPayment
        {
            get => Data.OrderBy(x => x.PayAmount).GroupBy(x => Math.Round(x.PayAmount, 5)).ToDictionary(x => $"{x.Key} BTC", y => y.Count());
        }

        public List<Payment> Data { get; set; } = new List<Payment>();

        public int TotalCount => Data.Count;
    }
}
