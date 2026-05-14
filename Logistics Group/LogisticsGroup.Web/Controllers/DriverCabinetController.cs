using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LogisticsGroup.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using LogisticsGroup.Infrastructure.Data;
using LogisticsGroup.Web.Services;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace LogisticsGroup.Web.Controllers
{
    [Authorize(Roles = "Driver")]
    public class DriverCabinetController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly WeatherApiService _weatherService;

        public DriverCabinetController(ApplicationDbContext context, WeatherApiService weatherService)
        {
            _context = context;
            _weatherService = weatherService;
        }

        public async Task<IActionResult> Index()
        {
            var userName = User.Identity?.Name;

            // 1. Тягнемо з бази і місто отримувача, І МІСТО ВІДПРАВНИКА!
            var activeFlights = await _context.Flights
                .Include(f => f.Vehicle)
                .Include(f => f.Driver)
                .Include(f => f.Route)
                .Include(f => f.Parcels)
                    .ThenInclude(p => p.ReceiverBranch).ThenInclude(b => b.City)
                .Include(f => f.Parcels)
                    .ThenInclude(p => p.SenderBranch).ThenInclude(b => b.City) // <--- ДОДАЛИ!
                .Where(f => f.Status == "Створено" || f.Status == "В дорозі")
                .ToListAsync();

            var flight = activeFlights.FirstOrDefault(f =>
                (f.Driver != null && GenerateDriverEmail(f.Driver.FullName) == userName) ||
                userName == "admin@novaposhta.com" ||
                userName == "morchuk985.mr@novaposhta.com");

            if (flight != null)
            {
                var cityCoordinates = new Dictionary<string, (double Lat, double Lon)>()
                {
                    { "Київ", (50.4501, 30.5234) },
                    { "Харків", (49.9935, 36.2304) },
                    { "Одеса", (46.4825, 30.7233) },
                    { "Дніпро", (48.4647, 35.0462) },
                    { "Львів", (49.8397, 24.0297) },
                    { "Запоріжжя", (47.8388, 35.1396) },
                    { "Полтава", (49.5883, 34.5514) },
                    { "Чернівці", (48.2915, 25.9352) },
                    { "Ужгород", (48.6208, 22.2879) },
                    { "Тернопіль", (49.5535, 25.5948) },
                    { "Рівне", (50.6199, 26.2516) }
                };

                // Дефолтні міста, якщо щось піде не так
                string destinationCity = "Київ";
                string originCity = "Львів";

                // Витягуємо реальні міста з першої посилки в фурі
                if (flight.Parcels != null && flight.Parcels.Any())
                {
                    var firstParcel = flight.Parcels.First();
                    if (firstParcel.ReceiverBranch?.City != null)
                        destinationCity = firstParcel.ReceiverBranch.City.Name;

                    if (firstParcel.SenderBranch?.City != null)
                        originCity = firstParcel.SenderBranch.City.Name;
                }

                double lat = 50.4501;
                double lon = 30.5234;

                if (cityCoordinates.ContainsKey(destinationCity))
                {
                    lat = cityCoordinates[destinationCity].Lat;
                    lon = cityCoordinates[destinationCity].Lon;
                }

                var weather = await _weatherService.GetCurrentWeatherAsync(lat, lon);

                // Віддаємо обидва міста на сторінку
                ViewBag.DestinationCity = destinationCity;
                ViewBag.OriginCity = originCity;

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
                    if (flight.Parcels != null) foreach (var parcel in flight.Parcels) parcel.Status = "Прибуло у відділення";
                }
                else if (newStatus == "В дорозі")
                {
                    if (flight.Driver != null) flight.Driver.Status = "В рейсі";
                    if (flight.Vehicle != null) flight.Vehicle.Status = "В рейсі";
                    if (flight.Parcels != null) foreach (var parcel in flight.Parcels) parcel.Status = "В дорозі";
                }
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ReportIssue(int flightId, string issueMsg)
        {
            var flight = await _context.Flights.FirstOrDefaultAsync(f => f.Id == flightId);
            if (flight != null)
            {
                flight.IssueMessage = issueMsg;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"⚠️ Логіста повідомлено про ситуацію: \"{issueMsg}\". Очікуйте на зв'язок!";
            }
            return RedirectToAction(nameof(Index));
        }

        private string GenerateDriverEmail(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "driver@logistics.com";
            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string lastName = parts.Length > 0 ? parts[0].ToLower() : "driver";
            string[] ukr = { "а", "б", "в", "г", "ґ", "д", "е", "є", "ж", "з", "и", "і", "ї", "й", "к", "л", "м", "н", "о", "п", "р", "с", "т", "у", "ф", "х", "ц", "ч", "ш", "щ", "ь", "ю", "я", "'" };
            string[] eng = { "a", "b", "v", "h", "g", "d", "e", "ye", "zh", "z", "y", "i", "yi", "y", "k", "l", "m", "n", "o", "p", "r", "s", "t", "u", "f", "kh", "ts", "ch", "sh", "shch", "", "yu", "ya", "" };
            for (int i = 0; i < ukr.Length; i++) lastName = lastName.Replace(ukr[i], eng[i]);
            lastName = Regex.Replace(lastName, @"[^a-z0-9]", "");
            return $"driver.{lastName}@logistics.com";
        }
    }
}