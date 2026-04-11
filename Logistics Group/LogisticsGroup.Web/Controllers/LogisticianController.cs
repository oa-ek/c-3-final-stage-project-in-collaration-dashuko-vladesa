using Microsoft.AspNetCore.Mvc;

namespace LogisticsGroup.Web.Controllers
{
    public class LogisticianController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}