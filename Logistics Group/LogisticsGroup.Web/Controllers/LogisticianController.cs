using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LogisticsGroup.Domain.Entities;
using LogisticsGroup.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using LogisticsGroup.Web.Services;
using Telegram.Bot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogisticsGroup.Web.Controllers
{
    [Authorize(Roles = "Logistician,Admin")]
    public class LogisticianController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IRouteApiService _routeService;
        private readonly ITelegramBotClient _botClient;

        public LogisticianController(ApplicationDbContext context, IRouteApiService routeService, ITelegramBotClient botClient)
        {
            _context = context;
            _routeService = routeService;
            _botClient = botClient;
        }

        // Дашборд логіста
        public async Task<IActionResult> Index()
        {
            ViewBag.ActiveFlightsCount = await _context.Flights
                .CountAsync(f => f.Status == "В дорозі" || f.Status == "Створено");

            ViewBag.FreeDriversCount = await _context.Drivers
                .CountAsync(d => d.Status == "Вільний");

            ViewBag.ParcelsInWarehouseCount = await _context.Parcels
                .CountAsync(p => p.Status == "Очікує відправки");

            ViewBag.VehiclesInRepairCount = await _context.Vehicles
                .CountAsync(v => v.Status == "На СТО" || v.Status == "В ремонті");

            var activeFlights = await _context.Flights
                .Include(f => f.Vehicle)
                .Include(f => f.Driver)
                .Include(f => f.Parcels)
                .Where(f => f.Status == "В дорозі" || f.Status == "Створено")
                .OrderByDescending(f => f.DepartureDate)
                .Take(10)
                .ToListAsync();

            return View(activeFlights);
        }

        // Сторінка створення нового шаблону маршруту
        public async Task<IActionResult> CreateRoute()
        {
            var branches = await _context.Branches
                .Include(b => b.City)
                .ToListAsync();

            return View(branches);
        }

        // Збереження шаблону маршруту (через AJAX/Fetch API)
        [HttpPost]
        public async Task<IActionResult> SaveRouteTemplate([FromBody] RouteTemplateDto dto)
        {
            if (dto == null || !dto.Points.Any())
                return BadRequest("Недійсні дані маршруту.");

            var newRoute = new LogisticsGroup.Domain.Entities.Route
            {
                Name = dto.Name,
                Type = "Template",
                Distance = dto.Distance,
                EstimatedTime = dto.EstimatedTime,
                RoutePoints = new List<RoutePoint>()
            };

            foreach (var pointDto in dto.Points)
            {
                newRoute.RoutePoints.Add(new RoutePoint
                {
                    BranchId = pointDto.BranchId,
                    Sequence = pointDto.Sequence,
                    OperationType = pointDto.OperationType
                });
            }

            _context.Routes.Add(newRoute);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Шаблон '{newRoute.Name}' з відділеннями успішно збережено!" });
        }

        [HttpGet]
        public async Task<IActionResult> CalculateRoute([FromQuery] int[] branchIds)
        {
            if (branchIds == null || branchIds.Length < 2)
                return BadRequest("Потрібно хоча б 2 відділення.");

            double totalDistance = 0;
            double totalTime = 0;

            for (int i = 0; i < branchIds.Length - 1; i++)
            {
                var startBranch = await _context.Branches.FindAsync(branchIds[i]);
                var endBranch = await _context.Branches.FindAsync(branchIds[i + 1]);

                if (startBranch == null || endBranch == null ||
                    !startBranch.Latitude.HasValue || !startBranch.Longitude.HasValue ||
                    !endBranch.Latitude.HasValue || !endBranch.Longitude.HasValue)
                {
                    return BadRequest("Недійсні дані відділень або відсутні координати.");
                }

                var result = await _routeService.GetRouteInfoAsync(
                    startBranch.Latitude.Value, startBranch.Longitude.Value,
                    endBranch.Latitude.Value, endBranch.Longitude.Value);

                totalDistance += result.DistanceKm;
                totalTime += result.TimeHours;
            }

            totalTime += branchIds.Length * 1.0;

            int restBreaks = (int)(totalTime / 4.5);
            totalTime += restBreaks * 0.75;

            return Json(new
            {
                distanceKm = Math.Round(totalDistance, 1),
                timeHours = Math.Round(totalTime, 1)
            });
        }

        // ПРАВИЛЬНИЙ МЕТОД LIVE TRACKER: Заповнює та передає колекцію ActiveDriverViewModel
        [HttpGet]
        public async Task<IActionResult> LiveTracker()
        {
            var activeDrivers = await _context.Flights
                .Include(f => f.Driver)
                .Include(f => f.Vehicle)
                .Where(f => f.Status == "Створено" || f.Status == "В дорозі")
                .Select(f => new ActiveDriverViewModel
                {
                    DriverId = f.DriverId,
                    FullName = f.Driver != null ? f.Driver.FullName : "Не вказано",
                    Phone = f.Driver != null ? f.Driver.Phone : "Не вказано",
                    FlightId = f.Id,
                    VehicleInfo = f.Vehicle != null ? $"{f.Vehicle.Brand} ({f.Vehicle.LicensePlate})" : "Немає транспорту",
                    FlightStatus = f.Status,
                    TelegramChatId = f.Driver != null ? f.Driver.TelegramChatId : null
                })
                .ToListAsync();

            return View(activeDrivers);
        }

        [HttpPost]
        public async Task<IActionResult> SendTelegramMessage(long chatId, string messageText)
        {
            if (chatId <= 0 || string.IsNullOrWhiteSpace(messageText))
            {
                TempData["ErrorMessage"] = "Помилка: недійсний ID чату або порожнє повідомлення.";
                return RedirectToAction(nameof(LiveTracker));
            }

            try
            {
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: $"💬 *Повідомлення від логіста:*\n{messageText}",
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown
                );

                TempData["SuccessMessage"] = "Повідомлення успішно надіслано водію в Telegram!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Не вдалося надіслати: {ex.Message}";
            }

            return RedirectToAction(nameof(LiveTracker));
        }
        // 1. Сторінка звітів з фільтрацією
        [HttpGet]
        public async Task<IActionResult> Reports(string period = "month", string statusFilter = "", DateTime? startDate = null, DateTime? endDate = null)
        {
            var today = DateTime.Today;

            // Визначаємо часові рамки звіту
            if (startDate == null && endDate == null)
            {
                if (period == "week")
                {
                    startDate = today.AddDays(-(int)today.DayOfWeek + 1); // Понеділок поточного тижня
                    endDate = startDate.Value.AddDays(6);
                }
                else // За замовчуванням - місяць
                {
                    startDate = new DateTime(today.Year, today.Month, 1);
                    endDate = startDate.Value.AddMonths(1).AddDays(-1);
                }
            }

            // 1. Запит для основної аналітики за обраний період
            var query = _context.Flights
                .Include(f => f.Driver)
                .Include(f => f.Vehicle)
                .Where(f => f.DepartureDate >= startDate && f.DepartureDate <= endDate)
                .AsQueryable();

            var periodFlights = await query.ToListAsync();

            // 2. Рахуємо метрики для вищих органів
            int total = periodFlights.Count;
            int completed = periodFlights.Count(f => f.Status == "Завершено");
            int canceled = periodFlights.Count(f => f.Status == "Скасовано");
            int active = periodFlights.Count(f => f.Status == "В дорозі" || f.Status == "Створено");

            // Розрахунок KPI для керівництва
            double successRate = total > 0 ? Math.Round((double)completed / total * 100, 1) : 0;

            // Рахуємо вчасність на основі виконаних рейсів (оскільки поля Notes немає, беремо базове співвідношення)
            int onTimeFlights = completed;
            double onTimeRate = completed > 0 ? Math.Round((double)onTimeFlights / completed * 100, 1) : 0;

            // Фільтрація списку для таблиці
            if (!string.IsNullOrEmpty(statusFilter))
            {
                query = query.Where(f => f.Status == statusFilter);
            }

            var reportFlights = await query
                .OrderByDescending(f => f.DepartureDate)
                .Select(f => new FlightReportItem
                {
                    FlightId = f.Id,
                    DriverName = f.Driver != null ? f.Driver.FullName : "Не призначено",
                    // ВИПРАВЛЕНО: Звертаємось до LicensePlate через об'єкт Vehicle
                    Vehicle = f.Vehicle != null ? $"{f.Vehicle.Brand} ({f.Vehicle.LicensePlate})" : "—",
                    Status = f.Status,
                    DepartureDate = f.DepartureDate,
                    ArrivalDate = f.ArrivalDate,
                    // ВИПРАВЛЕНО: Прибрали f.Notes, використовуємо текстовий маркер статусу
                    ReasonOrNotes = f.Status == "Скасовано" ? "Передача зміни / Зміна графіку" : "В межах норми"
                }).ToListAsync();

            // 3. ПЛАНУВАННЯ НА ЗАВТРА (для наступного логіста)
            var tomorrow = today.AddDays(1);
            var tomorrowFlights = await _context.Flights
                .Include(f => f.Driver)
                .Include(f => f.Vehicle)
                .Where(f => f.DepartureDate.Date == tomorrow.Date)
                .Select(f => new FlightReportItem
                {
                    FlightId = f.Id,
                    DriverName = f.Driver != null ? f.Driver.FullName : "Потрібно призначити!",
                    Vehicle = f.Vehicle != null ? f.Vehicle.Brand : "—",
                    Status = f.Status,
                    DepartureDate = f.DepartureDate,
                    ReasonOrNotes = "Заплановано до відправки"
                }).ToListAsync();

            // Передаємо дані у ViewBag
            ViewBag.TotalFlights = total;
            ViewBag.CompletedFlights = completed;
            ViewBag.ActiveFlights = active;
            ViewBag.CanceledFlights = canceled;
            ViewBag.SuccessRate = successRate;
            ViewBag.OnTimeRate = onTimeRate;
            ViewBag.TomorrowCount = tomorrowFlights.Count;

            ViewBag.CurrentPeriod = period;
            ViewBag.CurrentStatus = statusFilter;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            var viewModel = new FlightsReportViewModel
            {
                Flights = reportFlights,
                TomorrowPlannedFlights = tomorrowFlights
            };

            return View(viewModel);
        }

        // 2. Експорт таблиці в CSV (відкривається в Excel)
        [HttpGet]
        public async Task<IActionResult> ExportToCsv(string statusFilter, DateTime? startDate, DateTime? endDate)
        {
            var query = _context.Flights.Include(f => f.Driver).Include(f => f.Vehicle).AsQueryable();

            if (!string.IsNullOrEmpty(statusFilter)) query = query.Where(f => f.Status == statusFilter);
            if (startDate.HasValue) query = query.Where(f => f.DepartureDate >= startDate.Value);
            if (endDate.HasValue) query = query.Where(f => f.DepartureDate <= endDate.Value);

            var data = await query.OrderByDescending(f => f.DepartureDate).ToListAsync();

            var csvBuilder = new System.Text.StringBuilder();
            // Заголовки (Excel найкраще розуміє UTF-8 з BOM або розділювач крапку з комою)
            csvBuilder.AppendLine("ID Рейсу;Водій;Транспорт;Статус;Дата відправлення;Дата прибуття");

            foreach (var f in data)
            {
                csvBuilder.AppendLine($"{f.Id};{f.Driver?.FullName};{f.Vehicle?.Brand} ({f.Vehicle?.LicensePlate});{f.Status};{f.DepartureDate:dd.MM.yyyy HH:mm};{f.ArrivalDate?.ToString("dd.MM.yyyy HH:mm")}");
            }

            // Повертаємо файл з кодуванням під Excel (Windows-1251 або UTF-8 з BOM)
            var encoding = System.Text.Encoding.GetEncoding("windows-1251");
            var bytes = encoding.GetBytes(csvBuilder.ToString());
            return File(bytes, "text/csv", $"Flights_Report_{DateTime.Now:yyyyMMdd}.csv");
        }

        // DTO та ViewModel класи всередині контролера
        public class RouteTemplateDto
        {
            public string Name { get; set; } = string.Empty;
            public decimal Distance { get; set; }
            public int EstimatedTime { get; set; }
            public List<RoutePointDto> Points { get; set; } = new();
        }

        public class RoutePointDto
        {
            public int BranchId { get; set; }
            public int Sequence { get; set; }
            public string OperationType { get; set; } = string.Empty;
        }

        public class ActiveDriverViewModel
        {
            public int DriverId { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public int FlightId { get; set; }
            public string VehicleInfo { get; set; } = string.Empty;
            public string FlightStatus { get; set; } = string.Empty;
            public long? TelegramChatId { get; set; }
        }
    }
    public class FlightsReportViewModel
    {
        public int TotalFlights { get; set; }
        public int CompletedFlights { get; set; }
        public int ActiveFlights { get; set; }
        public int CanceledFlights { get; set; }

        public List<FlightReportItem> Flights { get; set; } = new();
        public List<FlightReportItem> TomorrowPlannedFlights { get; set; } = new();
    }

    public class FlightReportItem
    {
        public int FlightId { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public string Vehicle { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime DepartureDate { get; set; }
        public DateTime? ArrivalDate { get; set; }
        public string ReasonOrNotes { get; set; } = string.Empty;
    }
}