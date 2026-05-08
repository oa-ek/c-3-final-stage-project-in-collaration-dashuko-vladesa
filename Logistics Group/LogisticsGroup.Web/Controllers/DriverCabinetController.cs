using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsGroup.Web.Controllers
{
    [Authorize(Roles = "Driver")]
    public class DriverCabinetController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}