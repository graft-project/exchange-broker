using Microsoft.AspNetCore.Mvc;

namespace ExchangeBroker.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Index", "DemoTerminalApp");
        }
    }
}
