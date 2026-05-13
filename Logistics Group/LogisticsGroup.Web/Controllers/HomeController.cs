using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LogisticsGroup.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;

namespace LogisticsGroup.Web.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string ttn)
        {
            if (!string.IsNullOrEmpty(ttn))
            {
                ViewBag.SearchTerm = ttn;

                var idString = ttn.ToUpper().Replace("TTN-", "").Trim();

                if (int.TryParse(idString, out int parcelId))
                {
                    var parcel = await _context.Parcels
                        .Include(p => p.SenderBranch)
                            .ThenInclude(b => b.City)
                        .Include(p => p.ReceiverBranch)
                            .ThenInclude(b => b.City)
                        .FirstOrDefaultAsync(p => p.Id == parcelId);

                    if (parcel != null)
                    {
                        ViewBag.TrackedParcel = parcel;
                    }
                    else
                    {
                        ViewBag.Error = "Посилку з таким номером не знайдено. Перевірте правильність ТТН.";
                    }
                }
                else
                {
                    ViewBag.Error = "Неправильний формат номеру. Використовуйте формат, наприклад: TTN-1";
                }
            }

            return View();
        }
    }
}