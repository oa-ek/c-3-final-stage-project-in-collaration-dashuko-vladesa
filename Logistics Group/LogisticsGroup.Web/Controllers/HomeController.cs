using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LogisticsGroup.Infrastructure.Data; // Перевір, чи правильний тут namespace до ApplicationDbContext

namespace LogisticsGroup.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Метод приймає параметр ttn з рядка пошуку
        public async Task<IActionResult> Index(string ttn)
        {
            if (!string.IsNullOrEmpty(ttn))
            {
                ViewBag.SearchTerm = ttn;

                // Витягуємо тільки цифри (якщо користувач ввів "TTN-15", беремо "15")
                var idString = ttn.ToUpper().Replace("TTN-", "").Trim();

                if (int.TryParse(idString, out int parcelId))
                {
                    // Шукаємо посилку в базі
                    var parcel = await _context.Parcels.FirstOrDefaultAsync(p => p.Id == parcelId);

                    if (parcel != null)
                    {
                        ViewBag.TrackedParcel = parcel; // Передаємо знайдену посилку у View
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