using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LogisticsGroup.Domain.Entities;
using LogisticsGroup.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using LogisticsGroup.Web.Services; 

namespace LogisticsGroup.Web.Controllers
{
    [Authorize(Roles = "Logistician,Admin")]
    public class LogisticianController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IRouteApiService _routeService;

        public LogisticianController(ApplicationDbContext context, IRouteApiService routeService)
        {
            _context = context;
            _routeService = routeService;
        }

        // Дашборд логіста
        public async Task<IActionResult> Index()
        {
            // 1. Рахуємо реальну статистику для карток
            ViewBag.ActiveFlightsCount = await _context.Flights
                .CountAsync(f => f.Status == "В дорозі" || f.Status == "Створено");

            ViewBag.FreeDriversCount = await _context.Drivers
                .CountAsync(d => d.Status == "Вільний");

            ViewBag.ParcelsInWarehouseCount = await _context.Parcels
                .CountAsync(p => p.Status == "Очікує відправки");

            ViewBag.VehiclesInRepairCount = await _context.Vehicles
                .CountAsync(v => v.Status == "На СТО" || v.Status == "В ремонті");

            // 2. Отримуємо список активних рейсів для таблиці
            var activeFlights = await _context.Flights
                .Include(f => f.Vehicle)
                .Include(f => f.Driver)
                .Include(f => f.Parcels)
                .Where(f => f.Status == "В дорозі" || f.Status == "Створено")
                .OrderByDescending(f => f.DepartureDate)
                .Take(10) // Беремо останні 10 рейсів для дашборду
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

            var newRoute = new Domain.Entities.Route
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

            // 1. Додаємо по 1 годині на кожне відділення (оформлення, завантаження/вивантаження)
            totalTime += branchIds.Length * 1.0;

            // 2. Водій має відпочивати 45 хв (0.75 год) кожні 4.5 години за кермом
            int restBreaks = (int)(totalTime / 4.5);
            totalTime += restBreaks * 0.75;

            return Json(new
            {
                distanceKm = Math.Round(totalDistance, 1),
                timeHours = Math.Round(totalTime, 1)
            });
        }

        // DTO класи для прийняття JSON
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
    }
}