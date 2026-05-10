using LogisticsGroup.Domain.Entities;
using LogisticsGroup.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

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

        // GET: Список усіх рейсів для логіста
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Завантажуємо всі рейси разом із даними про машину, водія та посилками
            var flights = await _context.Flights
                .Include(f => f.Vehicle)
                .Include(f => f.Driver)
                .Include(f => f.Parcels)
                .OrderByDescending(f => f.DepartureDate) // Найновіші зверху
                .ToListAsync();

            return View(flights);
        }

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

            // Завантажуємо дані для випадаючих списків (FullName вже виправлено)
            ViewBag.Drivers = new SelectList(await _context.Drivers.ToListAsync(), "Id", "FullName");

            // Для машин виведемо номер, марку та вантажопідйомність
            var vehicles = await _context.Vehicles.Select(v => new
            {
                Id = v.Id,
                Info = $"{v.LicensePlate} ({v.Brand}, {v.Capacity} т)"
            }).ToListAsync();

            ViewBag.Vehicles = new SelectList(vehicles, "Id", "Info");
            ViewBag.SelectedParcelIds = selectedParcels;

            return View(parcels);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFlight(int vehicleId, int driverId, int[] parcelIds)
        {
            // 1. Створюємо рейс
            var flight = new Flight
            {
                VehicleId = vehicleId,
                DriverId = driverId,
                DepartureDate = DateTime.Now,
                Status = "В дорозі"
            };

            _context.Flights.Add(flight);
            await _context.SaveChangesAsync();

            // 2. Оновлюємо посилки
            var parcels = await _context.Parcels.Where(p => parcelIds.Contains(p.Id)).ToListAsync();
            foreach (var parcel in parcels)
            {
                parcel.FlightId = flight.Id;
                parcel.Status = "В дорозі";
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Рейс успішно сформовано! Номер рейсу: {flight.Id}";

            // Змінив редирект, щоб перекидало на список рейсів
            return RedirectToAction("Index");
        }

        // ДОДАНО: Метод для відображення деталей конкретного рейсу
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Підтягуємо рейс з усіма пов'язаними даними (водій, транспорт, посилки, міста)
            var flight = await _context.Flights
                .Include(f => f.Driver)
                .Include(f => f.Vehicle)
                .Include(f => f.Parcels)
                    .ThenInclude(p => p.ReceiverBranch)
                        .ThenInclude(b => b.City)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (flight == null)
            {
                return NotFound();
            }

            return View(flight);
        }
    }
}