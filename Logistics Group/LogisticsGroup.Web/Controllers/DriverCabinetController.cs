using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LogisticsGroup.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using LogisticsGroup.Infrastructure.Data;
using LogisticsGroup.Web.Services; // Підключаємо папку з сервісами API
using System.Threading.Tasks;

namespace LogisticsGroup.Web.Controllers
{
    [Authorize(Roles = "Driver")]
    public class DriverCabinetController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly WeatherApiService _weatherService; // 1. Додаємо сервіс погоди

        // 2. Інжектимо сервіс у конструктор
        public DriverCabinetController(ApplicationDbContext context, WeatherApiService weatherService)
        {
            _context = context;
            _weatherService = weatherService;
        }

        public async Task<IActionResult> Index()
        {
            var userName = User.Identity?.Name;

            var flight = await _context.Flights
                .Include(f => f.Vehicle)
                .Include(f => f.Driver)
                .Include(f => f.Parcels)
                .FirstOrDefaultAsync(f => (f.Driver.FullName == userName || userName == "admin@test.com" || userName == "morchuk985.mr@novaposhta.com")
                                          && (f.Status == "Створено" || f.Status == "В дорозі"));

            if (flight == null)
            {
                flight = await _context.Flights
                    .Include(f => f.Vehicle)
                    .Include(f => f.Driver)
                    .Include(f => f.Parcels)
                    .FirstOrDefaultAsync(f => f.Status == "Створено" || f.Status == "В дорозі");
            }

            // 3. ВИКЛИК ЗОВНІШНЬОГО API (Завдання лабораторної)
            if (flight != null)
            {
                // Симулюємо координати пункту призначення (наприклад, Київ) для демонстрації API
                var weather = await _weatherService.GetCurrentWeatherAsync(50.4501, 30.5234);

                if (weather != null)
                {
                    ViewBag.WeatherTemp = weather.Value.Temp;
                    ViewBag.WeatherDesc = weather.Value.Description;
                }
                else
                {
                    ViewBag.WeatherDesc = "Дані недоступні";
                }
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
                    if (flight.Driver != null) flight.Driver.Status = "Вільний";
                    if (flight.Vehicle != null) flight.Vehicle.Status = "Вільний";

                    if (flight.Parcels != null)
                    {
                        foreach (var parcel in flight.Parcels) parcel.Status = "Прибуло у відділення";
                    }
                }
                else if (newStatus == "В дорозі")
                {
                    if (flight.Driver != null) flight.Driver.Status = "В рейсі";
                    if (flight.Vehicle != null) flight.Vehicle.Status = "В рейсі";

                    if (flight.Parcels != null)
                    {
                        foreach (var parcel in flight.Parcels) parcel.Status = "В дорозі";
                    }
                }
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}