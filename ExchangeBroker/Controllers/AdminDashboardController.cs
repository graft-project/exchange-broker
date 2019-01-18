using Graft.Infrastructure.Watcher;
using Microsoft.AspNetCore.Mvc;

namespace ExchangeBroker.Controllers
{
    public class AdminDashboardController : Controller
    {
        readonly WatcherService watcher;

        public AdminDashboardController(WatcherService watcher)
        {
            this.watcher = watcher;
        }

        public IActionResult Index()
        {
            return View(watcher.Services);
        }
    }
}