using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LogisticsGroup.Domain.Entities;
using LogisticsGroup.Infrastructure.Data; // Переконайся, що тут правильний using для твого ApplicationDbContext

namespace LogisticsGroup.Web.Controllers
{
    public class LogisticianController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LogisticianController(ApplicationDbContext context)
        {
            _context = context;
        }

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
                .Include(f => f.Parcels) // Підтягуємо посилки, щоб знати їх кількість
                .Where(f => f.Status == "В дорозі" || f.Status == "Створено")
                .OrderByDescending(f => f.DepartureDate)
                .Take(10) // Беремо останні 10 рейсів для дашборду
                .ToListAsync();

            return View(activeFlights);
        }
    }
}