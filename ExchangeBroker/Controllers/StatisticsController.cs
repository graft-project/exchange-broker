using ExchangeBroker.Data;
using ExchangeBroker.Models;
using Graft.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task<IActionResult> Index()
        {
            ISearchModel model;

            model = new SearchModelExchange
            {
                Data = await _context.Exchange.ToListAsync()
            };

            return View(model);
        }
    }

    public interface ISearchModel
    {
        int TotalCount { get; }

        string ItemTypeTo { get; }

        int GetStatusCount(PaymentStatus status);

        int FailedCount { get; }

        int InProgressCount { get; }

        int SuccessfulCount { get; }

        Dictionary<string, int> DataSortedByDays { get; }

        Dictionary<string, int> DataSortedByPayment { get; }
    }

    public class SearchModelExchange : ISearchModel
    {
        public string ItemTypeTo { get => "Exchanges"; }

        public int GetStatusCount(PaymentStatus status)
        {
            return Data.Count(x => x.InTxStatus == status);
        }

        public int FailedCount
        {
            get => Data.Count(x => new[]
            {
                PaymentStatus.TimedOut,
                PaymentStatus.RejectedByWallet,
                PaymentStatus.RejectedByPOS,
                PaymentStatus.NotEnoughAmount,
                PaymentStatus.Fail,
                PaymentStatus.DoubleSpend
            }.Contains(x.InTxStatus));
        }

        public int InProgressCount
        {
            get => Data.Count(x => new[]
            {
                PaymentStatus.InProgress,
                PaymentStatus.New,
                PaymentStatus.Waiting
            }.Contains(x.InTxStatus));
        }

        public int SuccessfulCount
        {
            get => Data.Count(x => new[]
            {
                PaymentStatus.Received,
                PaymentStatus.Confirmed
            }.Contains(x.InTxStatus));
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
}
