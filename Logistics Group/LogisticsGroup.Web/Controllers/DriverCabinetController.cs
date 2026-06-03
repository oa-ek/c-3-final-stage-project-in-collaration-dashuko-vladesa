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
using DocumentFormat.OpenXml.Bibliography;

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
            var userName = User.Identity?.Name?.ToLower().Trim();

            // 1. Тягнемо з бази активні рейси
            var activeFlights = await _context.Flights
                .Include(f => f.Vehicle)
                .Include(f => f.Driver)
                .Include(f => f.Route)
                .Include(f => f.Parcels)
                    .ThenInclude(p => p.ReceiverBranch).ThenInclude(b => b.City)
                .Include(f => f.Parcels)
                    .ThenInclude(p => p.SenderBranch).ThenInclude(b => b.City)
                .Where(f => f.Status == "Створено" || f.Status == "В дорозі")
                .ToListAsync();

            // 2. Гнучка перевірка водія (підтримує як старий формат, так і новий @novaposhta.com)
            var flight = activeFlights.FirstOrDefault(f =>
                (f.Driver != null && IsDriverEmailMatch(f.Driver.FullName, userName)) ||
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

                string destinationCity = "Київ";
                string originCity = "Львів";

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

                // Генеруємо простий унікальний код авторизації для водія (наприклад: 1000 + ID водія)
                ViewBag.DriverAuthCode = 1000 + flight.DriverId;
            }
            else
            {
                ViewBag.DriverAuthCode = null;
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
                TempData["SuccessMessage"] = $"⚠️ Логіста повідомлено про 상황: \"{issueMsg}\". Очікуйте на зв'язок!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool IsDriverEmailMatch(string fullName, string userName)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(userName)) return false;

            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return false;

            string lastNameEng = TransliterateWord(parts[0]);
            string firstNameEng = parts.Length > 1 ? TransliterateWord(parts[1]) : "";

            string novaPoshtaFormat = $"{lastNameEng}.{firstNameEng}@novaposhta.com";
            string legacyFormat = $"driver.{lastNameEng}@logistics.com";

            return userName == novaPoshtaFormat || userName == legacyFormat;
        }

        private string TransliterateWord(string word)
        {
            word = word.ToLower();
            string[] ukr = { "а", "б", "в", "г", "ґ", "д", "е", "є", "ж", "з", "и", "і", "ї", "й", "к", "л", "м", "н", "о", "п", "р", "с", "т", "у", "ф", "х", "ц", "ч", "ш", "щ", "ь", "ю", "я", "'" };
            string[] eng = { "a", "b", "v", "h", "g", "d", "e", "ye", "zh", "z", "y", "i", "yi", "y", "k", "l", "m", "n", "o", "p", "r", "s", "t", "u", "f", "kh", "ts", "ch", "sh", "shch", "", "yu", "ya", "" };

            for (int i = 0; i < ukr.Length; i++) word = word.Replace(ukr[i], eng[i]);
            return Regex.Replace(word, @"[^a-z0-9]", "");
        }
    }
}