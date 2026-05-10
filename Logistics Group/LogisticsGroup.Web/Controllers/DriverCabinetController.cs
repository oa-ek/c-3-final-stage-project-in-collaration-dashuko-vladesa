using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LogisticsGroup.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using LogisticsGroup.Infrastructure.Data; // Переконайся, що namespace для ApplicationDbContext вірний

namespace LogisticsGroup.Web.Controllers
{
    [Authorize(Roles = "Driver")]
    public class DriverCabinetController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DriverCabinetController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userName = User.Identity?.Name;

            // Шукаємо активний рейс для поточного водія
            // Для тестування: якщо водія по імені не знайдено, беремо будь-який перший активний рейс
            var flight = await _context.Flights
                .Include(f => f.Vehicle)
                .Include(f => f.Driver)
                .Include(f => f.Parcels)
                .FirstOrDefaultAsync(f => (f.Driver.FullName == userName || userName == "admin@test.com")
                                          && (f.Status == "Створено" || f.Status == "В дорозі"));

            if (flight == null)
            {
                flight = await _context.Flights
                    .Include(f => f.Vehicle)
                    .Include(f => f.Driver)
                    .Include(f => f.Parcels)
                    .FirstOrDefaultAsync(f => f.Status == "Створено" || f.Status == "В дорозі");
            }

            return View(flight);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string newStatus)
        {
            var flight = await _context.Flights
                .Include(f => f.Parcels)
                .Include(f => f.Driver)
                .Include(f => f.Vehicle)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (flight != null)
            {
                flight.Status = newStatus;

                if (newStatus == "Доставлено")
                {
                    // Рейс завершено: звільняємо водія та авто
                    if (flight.Driver != null) flight.Driver.Status = "Вільний";
                    if (flight.Vehicle != null) flight.Vehicle.Status = "Вільний";

                    // Посилки прибули
                    if (flight.Parcels != null)
                    {
                        foreach (var parcel in flight.Parcels)
                        {
                            parcel.Status = "Прибуло у відділення";
                        }
                    }
                }
                else if (newStatus == "В дорозі")
                {
                    // Рейс почався
                    if (flight.Driver != null) flight.Driver.Status = "В рейсі";
                    if (flight.Vehicle != null) flight.Vehicle.Status = "В рейсі";

                    if (flight.Parcels != null)
                    {
                        foreach (var parcel in flight.Parcels)
                        {
                            parcel.Status = "В дорозі";
                        }
                    }
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}