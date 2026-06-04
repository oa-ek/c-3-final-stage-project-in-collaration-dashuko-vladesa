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
using System.Text;
using System.Threading.Tasks;
using SelectPdf;

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
            ViewBag.ActiveFlightsCount = await _context.Flights.CountAsync(f => f.Status == "В дорозі" || f.Status == "Створено");
            ViewBag.FreeDriversCount = await _context.Drivers.CountAsync(d => d.Status == "Вільний");
            ViewBag.ParcelsInWarehouseCount = await _context.Parcels.CountAsync(p => p.Status == "Очікує відправки");
            ViewBag.VehiclesInRepairCount = await _context.Vehicles.CountAsync(v => v.Status == "На СТО" || v.Status == "В ремонті");

            var activeFlights = await _context.Flights
                .Include(f => f.Vehicle).Include(f => f.Driver).Include(f => f.Parcels)
                .Where(f => f.Status == "В дорозі" || f.Status == "Створено")
                .OrderByDescending(f => f.DepartureDate).Take(10).ToListAsync();

            return View(activeFlights);
        }

        // === НОВИЙ МЕТОД: ЧАТ З ВОДІЄМ З ГОЛОВНОЇ СТОРІНКИ ===
        [HttpPost]
        public async Task<IActionResult> SendMessageToDriver(int driverId, string message)
        {
            var driver = await _context.Drivers.FindAsync(driverId);

            if (driver != null && driver.TelegramChatId != 0)
            {
                try
                {
                    // Використовуємо вже налаштований _botClient
                    await _botClient.SendMessage(
                        chatId: driver.TelegramChatId,
                        text: $"⚠️ *Повідомлення від логіста:*\n\n{message}",
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);

                    TempData["SuccessMessage"] = $"Повідомлення успішно відправлено водію {driver.FullName}!";
                }
                catch (Exception)
                {
                    TempData["ErrorMessage"] = "Помилка відправки: Водій заблокував бота або сталася помилка мережі.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Цей водій ще не підключив Telegram-бота!";
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> CreateRoute()
        {
            var branches = await _context.Branches.Include(b => b.City).ToListAsync();
            return View(branches);
        }

        [HttpPost]
        public async Task<IActionResult> SaveRouteTemplate([FromBody] RouteTemplateDto dto)
        {
            if (dto == null || !dto.Points.Any()) return BadRequest("Недійсні дані маршруту.");
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
                newRoute.RoutePoints.Add(new RoutePoint { BranchId = pointDto.BranchId, Sequence = pointDto.Sequence, OperationType = pointDto.OperationType });
            }
            _context.Routes.Add(newRoute);
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Шаблон '{newRoute.Name}' з відділеннями успішно збережено!" });
        }

        [HttpGet]
        public async Task<IActionResult> CalculateRoute([FromQuery] int[] branchIds)
        {
            if (branchIds == null || branchIds.Length < 2) return BadRequest("Потрібно хоча б 2 відділення.");
            double totalDistance = 0, totalTime = 0;
            for (int i = 0; i < branchIds.Length - 1; i++)
            {
                var startBranch = await _context.Branches.FindAsync(branchIds[i]);
                var endBranch = await _context.Branches.FindAsync(branchIds[i + 1]);
                if (startBranch?.Latitude == null || endBranch?.Latitude == null) return BadRequest("Недійсні дані відділень.");

                var result = await _routeService.GetRouteInfoAsync(startBranch.Latitude.Value, startBranch.Longitude.Value, endBranch.Latitude.Value, endBranch.Longitude.Value);
                totalDistance += result.DistanceKm; totalTime += result.TimeHours;
            }
            totalTime += branchIds.Length * 1.0;
            totalTime += (int)(totalTime / 4.5) * 0.75;

            return Json(new { distanceKm = Math.Round(totalDistance, 1), timeHours = Math.Round(totalTime, 1) });
        }

        [HttpGet]
        public async Task<IActionResult> LiveTracker()
        {
            var activeDrivers = await _context.Flights.Include(f => f.Driver).Include(f => f.Vehicle)
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
                }).ToListAsync();

            return View(activeDrivers);
        }

        [HttpPost]
        public async Task<IActionResult> SendTelegramMessage(long chatId, string messageText)
        {
            if (chatId <= 0 || string.IsNullOrWhiteSpace(messageText)) { TempData["ErrorMessage"] = "Помилка"; return RedirectToAction(nameof(LiveTracker)); }
            try
            {
                await _botClient.SendMessage(chatId, $"💬 *Повідомлення від логіста:*\n{messageText}", parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                TempData["SuccessMessage"] = "Повідомлення надіслано!";
            }
            catch (Exception ex) { TempData["ErrorMessage"] = $"Помилка: {ex.Message}"; }
            return RedirectToAction(nameof(LiveTracker));
        }

        // --- БЛОК ЗВІТІВ ---
        [HttpGet]
        public async Task<IActionResult> Reports(string period = "month", string statusFilter = "", DateTime? startDate = null, DateTime? endDate = null)
        {
            var today = DateTime.Today;
            if (startDate == null && endDate == null)
            {
                if (period == "week") { startDate = today.AddDays(-(int)today.DayOfWeek + 1); endDate = startDate.Value.AddDays(6); }
                else { startDate = new DateTime(today.Year, today.Month, 1); endDate = startDate.Value.AddMonths(1).AddDays(-1); }
            }

            var query = _context.Flights.Include(f => f.Driver).Include(f => f.Vehicle).Where(f => f.DepartureDate >= startDate && f.DepartureDate <= endDate).AsQueryable();
            var periodFlights = await query.ToListAsync();

            int total = periodFlights.Count;
            int completed = periodFlights.Count(f => f.Status == "Завершено");
            int canceled = periodFlights.Count(f => f.Status == "Скасовано");
            int active = periodFlights.Count(f => f.Status == "В дорозі" || f.Status == "Створено");
            double successRate = total > 0 ? Math.Round((double)completed / total * 100, 1) : 0;
            double onTimeRate = completed > 0 ? Math.Round((double)completed / completed * 100, 1) : 0;

            if (!string.IsNullOrEmpty(statusFilter)) query = query.Where(f => f.Status == statusFilter);

            var reportFlights = await query.OrderByDescending(f => f.DepartureDate).Select(f => new FlightReportItem
            {
                FlightId = f.Id,
                DriverName = f.Driver != null ? f.Driver.FullName : "Не призначено",
                Vehicle = f.Vehicle != null ? $"{f.Vehicle.Brand} ({f.Vehicle.LicensePlate})" : "—",
                Status = f.Status,
                DepartureDate = f.DepartureDate,
                ArrivalDate = f.ArrivalDate,
                ReasonOrNotes = f.Status == "Скасовано" ? "Передача зміни / Форс-мажор" : "В межах норми"
            }).ToListAsync();

            var tomorrow = today.AddDays(1);
            var tomorrowFlights = await _context.Flights.Include(f => f.Driver).Include(f => f.Vehicle).Where(f => f.DepartureDate.Date == tomorrow.Date)
                .Select(f => new FlightReportItem { FlightId = f.Id, DriverName = f.Driver != null ? f.Driver.FullName : "Потрібно призначити!", Vehicle = f.Vehicle != null ? f.Vehicle.Brand : "—", Status = f.Status, DepartureDate = f.DepartureDate, ReasonOrNotes = "Заплановано" }).ToListAsync();

            ViewBag.TotalFlights = total; ViewBag.CompletedFlights = completed; ViewBag.ActiveFlights = active; ViewBag.CanceledFlights = canceled;
            ViewBag.SuccessRate = successRate; ViewBag.OnTimeRate = onTimeRate; ViewBag.TomorrowCount = tomorrowFlights.Count;
            ViewBag.CurrentPeriod = period; ViewBag.CurrentStatus = statusFilter; ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd"); ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(new FlightsReportViewModel { Flights = reportFlights, TomorrowPlannedFlights = tomorrowFlights });
        }

        [HttpGet]
        public async Task<IActionResult> ExportToCsv(string statusFilter, DateTime? startDate, DateTime? endDate)
        {
            var query = _context.Flights.Include(f => f.Driver).Include(f => f.Vehicle).AsQueryable();
            if (!string.IsNullOrEmpty(statusFilter)) query = query.Where(f => f.Status == statusFilter);
            if (startDate.HasValue) query = query.Where(f => f.DepartureDate >= startDate.Value);
            if (endDate.HasValue) query = query.Where(f => f.DepartureDate <= endDate.Value);
            var data = await query.OrderByDescending(f => f.DepartureDate).ToListAsync();

            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("ID Рейсу;Водій;Транспорт;Статус;Дата відправлення;Дата прибуття");
            foreach (var f in data) csvBuilder.AppendLine($"{f.Id};{f.Driver?.FullName};{f.Vehicle?.Brand} ({f.Vehicle?.LicensePlate});{f.Status};{f.DepartureDate:dd.MM.yyyy HH:mm};{f.ArrivalDate?.ToString("dd.MM.yyyy HH:mm")}");
            return File(System.Text.Encoding.GetEncoding("windows-1251").GetBytes(csvBuilder.ToString()), "text/csv", $"Flights_Report_{DateTime.Now:yyyyMMdd}.csv");
        }

        // === МЕТОД ДЛЯ ГЕНЕРАЦІЇ КРАСИВОГО PDF ===
        [HttpGet]
        public async Task<IActionResult> DownloadPdfReport(string period = "month", string statusFilter = "", DateTime? startDate = null, DateTime? endDate = null)
        {
            var today = DateTime.Today;
            if (startDate == null && endDate == null)
            {
                if (period == "week") { startDate = today.AddDays(-(int)today.DayOfWeek + 1); endDate = startDate.Value.AddDays(6); }
                else { startDate = new DateTime(today.Year, today.Month, 1); endDate = startDate.Value.AddMonths(1).AddDays(-1); }
            }

            var query = _context.Flights.Include(f => f.Driver).Include(f => f.Vehicle)
                .Where(f => f.DepartureDate >= startDate && f.DepartureDate <= endDate).AsQueryable();

            if (!string.IsNullOrEmpty(statusFilter)) query = query.Where(f => f.Status == statusFilter);

            var allFlights = await query.ToListAsync();

            int total = allFlights.Count;
            int completed = allFlights.Count(f => f.Status == "Завершено");
            int canceled = allFlights.Count(f => f.Status == "Скасовано");
            double successRate = total > 0 ? Math.Round((double)completed / total * 100, 1) : 0;

            string logisticianName = User.Identity?.Name ?? "Ільчук В. М.";

            var sb = new StringBuilder();
            sb.Append(@"
            <!DOCTYPE html>
            <html lang='uk'>
            <head>
                <meta charset='utf-8'>
                <style>
                    body { font-family: 'Arial', sans-serif; color: #1f2937; margin: 20px; }
                    .header-container { border-bottom: 2px solid #ed3237; padding-bottom: 12px; margin-bottom: 20px; }
                    .company-badge { color: #ed3237; font-weight: bold; font-size: 14pt; text-transform: uppercase; margin-bottom: 5px; }
                    h1 { color: #111827; font-size: 18pt; margin: 5px 0; font-weight: 700; }
                    .subtitle { color: #4b5563; font-size: 10pt; font-style: italic; }
                    .meta-table { width: 100%; border-collapse: collapse; margin-bottom: 25px; background-color: #f9fafb; border: 1px solid #e5e7eb; }
                    .meta-table td { padding: 10px 12px; font-size: 9.5pt; color: #374151; width: 50%; vertical-align: top; }
                    .meta-table td strong { color: #111827; }
                    h2 { color: #111827; font-size: 13pt; font-weight: 600; margin-top: 25px; margin-bottom: 12px; border-left: 4px solid #ed3237; padding-left: 10px; page-break-after: avoid; }
                    p { font-size: 10pt; margin-bottom: 15px; color: #374151; }
                    .kpi-table { width: 100%; border-collapse: separate; border-spacing: 10px 0; margin: 15px -10px 25px -10px; }
                    .kpi-cell { width: 25%; vertical-align: top; }
                    .kpi-card { background-color: #f3f4f6; border: 1px solid #e5e7eb; border-top: 4px solid #4b5563; padding: 12px 10px; text-align: center; border-radius: 4px; }
                    .kpi-card.success { background-color: #f0fdf4; border-color: #bbf7d0; border-top-color: #16a34a; }
                    .kpi-card.danger { background-color: #fef2f2; border-color: #fecaca; border-top-color: #ed3237; }
                    .kpi-card.accent { background-color: #eff6ff; border-color: #bfdbfe; border-top-color: #2563eb; }
                    .kpi-val { font-size: 18pt; font-weight: bold; color: #111827; margin-bottom: 4px; }
                    .kpi-card.success .kpi-val { color: #15803d; }
                    .kpi-card.danger .kpi-val { color: #ed3237; }
                    .kpi-card.accent .kpi-val { color: #1d4ed8; }
                    .kpi-lbl { font-size: 8.5pt; color: #4b5563; font-weight: 500; text-transform: uppercase; }
                    .data-table { width: 100%; border-collapse: collapse; margin-top: 10px; margin-bottom: 20px; }
                    .data-table th { background-color: #374151; color: #ffffff; font-weight: 600; font-size: 9.5pt; text-align: left; padding: 10px 12px; border: 1px solid #374151; }
                    .data-table td { padding: 9px 12px; font-size: 9.5pt; border: 1px solid #e5e7eb; vertical-align: middle; }
                    .data-table tr:nth-child(even) { background-color: #f9fafb; }
                    .badge-success { background-color: #dcfce7; color: #15803d; padding: 3px 8px; font-size: 8pt; font-weight: 600; border-radius: 3px; text-transform: uppercase;}
                    .badge-danger { color: #ed3237; font-weight: bold; }
                    .signature-section { margin-top: 40px; width: 100%; border-collapse: collapse; page-break-inside: avoid; }
                    .signature-section td { width: 50%; font-size: 10pt; padding-top: 30px; }
                    .line { width: 200px; border-bottom: 1px solid #9ca3af; margin-top: 25px; display: inline-block; }
                </style>
            </head>
            <body>");

            sb.Append($@"
            <div class='header-container'>
                <div class='company-badge'>🏢 LogisticsGroup Web</div>
                <h1>АНАЛІТИЧНИЙ ЗВІТ ЕФЕКТИВНОСТІ РЕЙСІВ</h1>
                <div class='subtitle'>Офіційний операційний документ системи управління флотом</div>
            </div>

            <table class='meta-table'>
                <tr>
                    <td>
                        <strong>Відповідальний логіст:</strong> {logisticianName}<br>
                        <strong>Департамент:</strong> Операційна логістика<br>
                    </td>
                    <td style='border-left: 1px solid #e5e7eb;'>
                        <strong>Дата формування:</strong> {DateTime.Now:dd.MM.yyyy HH:mm}<br>
                        <strong>Звітний період:</strong> з {startDate:dd.MM.yyyy} по {endDate:dd.MM.yyyy}<br>
                    </td>
                </tr>
            </table>

            <h2>1. Ключові показники ефективності (KPI)</h2>
            <table class='kpi-table'>
                <tr>
                    <td class='kpi-cell'><div class='kpi-card'><div class='kpi-val'>{total}</div><div class='kpi-lbl'>Всього рейсів</div></div></td>
                    <td class='kpi-cell'><div class='kpi-card success'><div class='kpi-val'>{completed}</div><div class='kpi-lbl'>Виконано вчасно</div></div></td>
                    <td class='kpi-cell'><div class='kpi-card danger'><div class='kpi-val'>{canceled}</div><div class='kpi-lbl'>Не встигли / Збій</div></div></td>
                    <td class='kpi-cell'><div class='kpi-card accent'><div class='kpi-val'>{successRate}%</div><div class='kpi-lbl'>Успішність (SLA)</div></div></td>
                </tr>
            </table>

            <h2>2. Аналіз проблемних / невиконаних рейсів</h2>
            <table class='data-table'>
                <thead>
                    <tr><th style='width: 15%;'>Код рейсу</th><th style='width: 35%;'>Транспортний засіб</th><th style='width: 50%;'>Причина затримки / Статус</th></tr>
                </thead>
                <tbody>");

            var failedFlights = allFlights.Where(f => f.Status == "Скасовано").ToList();
            if (!failedFlights.Any())
            {
                sb.Append("<tr><td colspan='3' style='text-align:center;'>Невиконаних рейсів за обраний період немає.</td></tr>");
            }
            else
            {
                foreach (var f in failedFlights)
                {
                    string vehicleInfo = f.Vehicle != null ? $"{f.Vehicle.Brand} ({f.Vehicle.LicensePlate})" : "Немає авто";
                    sb.Append($"<tr><td><strong>FLT-{f.Id}</strong></td><td>{vehicleInfo}</td><td><span class='badge-danger'>Форс-мажор / Передача зміни</span></td></tr>");
                }
            }

            sb.Append(@"
                </tbody>
            </table>

            <h2>3. Реєстр успішно виконаних рейсів</h2>
            <table class='data-table'>
                <thead>
                    <tr><th style='width: 15%;'>Код рейсу</th><th style='width: 35%;'>Водій</th><th style='width: 25%;'>Дата</th><th style='width: 25%;'>Статус</th></tr>
                </thead>
                <tbody>");

            var successFlights = allFlights.Where(f => f.Status == "Завершено").Take(20).ToList();
            if (!successFlights.Any())
            {
                sb.Append("<tr><td colspan='4' style='text-align:center;'>Успішних рейсів за період немає.</td></tr>");
            }
            else
            {
                foreach (var f in successFlights)
                {
                    sb.Append($"<tr><td>FLT-{f.Id}</td><td>{f.Driver?.FullName}</td><td>{f.DepartureDate:dd.MM.yyyy}</td><td><span class='badge-success'>✓ Вчасно</span></td></tr>");
                }
            }

            sb.Append($@"
                </tbody>
            </table>

            <table class='signature-section'>
                <tr>
                    <td>Звіт підготував (Логіст):<br><span class='line'></span><br>{logisticianName}</td>
                    <td style='text-align: right;'>Затверджено керівником:<br><span class='line'></span><br>Директор з логістики</td>
                </tr>
            </table>

            </body>
            </html>");

            HtmlToPdf converter = new HtmlToPdf();
            converter.Options.PdfPageSize = PdfPageSize.A4;
            converter.Options.MarginTop = 15;
            converter.Options.MarginBottom = 15;
            converter.Options.MarginLeft = 15;
            converter.Options.MarginRight = 15;

            // Запобігання проблемам з кодуванням кирилиці
            converter.Options.WebPageFixedSize = false;
            converter.Options.DrawBackground = true;

            PdfDocument doc = converter.ConvertHtmlString(sb.ToString());
            byte[] pdfBytes = doc.Save();
            doc.Close();

            return File(pdfBytes, "application/pdf", $"Logistics_Official_Report_{DateTime.Now:yyyyMMdd}.pdf");
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