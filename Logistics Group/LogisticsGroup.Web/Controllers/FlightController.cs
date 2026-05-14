using LogisticsGroup.Domain.Entities;
using LogisticsGroup.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogisticsGroup.Web.Controllers
{
    [Authorize(Roles = "Logistician,Admin")]
    public class FlightController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FlightController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Список усіх рейсів
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var flights = await _context.Flights
                .Include(f => f.Vehicle)
                .Include(f => f.Driver)
                .Include(f => f.Parcels)
                .OrderByDescending(f => f.DepartureDate)
                .ToListAsync();

            return View(flights);
        }

        // GET: Підготовка до створення рейсу (вибір машини, водія, маршруту)
        [HttpGet]
        public async Task<IActionResult> PrepareFlight(int[] selectedParcels)
        {
            if (selectedParcels == null || selectedParcels.Length == 0)
            {
                TempData["Error"] = "Ви не вибрали жодної посилки для рейсу!";
                return RedirectToAction("Index", "Parcel");
            }

            var parcels = await _context.Parcels
                .Include(p => p.ReceiverBranch).ThenInclude(b => b.City)
                .Where(p => selectedParcels.Contains(p.Id))
                .ToListAsync();

            // Шукаємо вільних водіїв
            var availableDrivers = await _context.Drivers
                .Where(d => d.Status != null && d.Status.Contains("Вільн"))
                .ToListAsync();
            ViewBag.Drivers = new SelectList(availableDrivers, "Id", "FullName");

            // Шукаємо вільні машини
            var availableVehicles = await _context.Vehicles
                .Where(v => v.Status != null && (v.Status.Contains("Вільн") || v.Status.Contains("Готов")))
                .Select(v => new
                {
                    v.Id,
                    Info = $"{v.LicensePlate} ({v.Brand}, {v.Capacity} т)"
                }).ToListAsync();
            ViewBag.Vehicles = new SelectList(availableVehicles, "Id", "Info");

            // Маршрути-шаблони
            var routes = await _context.Routes.Where(r => r.Type == "Template").ToListAsync();
            ViewBag.Routes = new SelectList(routes, "Id", "Name");

            ViewBag.SelectedParcelIds = selectedParcels;

            return View(parcels);
        }

        // POST: Створення рейсу
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFlight(int vehicleId, int driverId, int routeId, int[] parcelIds)
        {
            var routeTemplate = await _context.Routes.FindAsync(routeId);

            var flight = new Flight
            {
                VehicleId = vehicleId,
                DriverId = driverId,
                RouteId = routeId,
                DepartureDate = DateTime.Now,
                ArrivalDate = DateTime.Now.AddHours(routeTemplate?.EstimatedTime ?? 0),
                Status = "В дорозі"
            };

            _context.Flights.Add(flight);
            await _context.SaveChangesAsync();

            // Оновлення посилок
            var parcels = await _context.Parcels.Where(p => parcelIds.Contains(p.Id)).ToListAsync();
            foreach (var parcel in parcels)
            {
                parcel.FlightId = flight.Id;
                parcel.Status = "В дорозі";
            }

            // Оновлення статусів машини та водія
            var vehicle = await _context.Vehicles.FindAsync(vehicleId);
            if (vehicle != null) vehicle.Status = "В рейсі";

            var driver = await _context.Drivers.FindAsync(driverId);
            if (driver != null) driver.Status = "В рейсі";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Рейс успішно сформовано! Номер рейсу: FLT-{flight.Id}";

            return RedirectToAction("Index");
        }

        // GET: Деталі рейсу
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var flight = await _context.Flights
                .Include(f => f.Driver)
                .Include(f => f.Vehicle)
                .Include(f => f.Route)
                .Include(f => f.Parcels)
                    .ThenInclude(p => p.ReceiverBranch)
                        .ThenInclude(b => b.City)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (flight == null) return NotFound();

            return View(flight);
        }

        // --- НОВИЙ МЕТОД: Очищення повідомлення про проблему (Ідея №1) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveIssue(int id)
        {
            var flight = await _context.Flights.FindAsync(id);

            if (flight == null)
            {
                return NotFound();
            }

            // Очищуємо поле з повідомленням
            flight.IssueMessage = null;

            // Зберігаємо зміни
            _context.Update(flight);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Статус оновлено: проблему вирішено, повідомлення видалено.";

            return RedirectToAction(nameof(Index));
        }
    }
}