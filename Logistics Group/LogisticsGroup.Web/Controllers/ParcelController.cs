using LogisticsGroup.Domain.Entities;
using LogisticsGroup.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LogisticsGroup.Web.Controllers
{
    // Доступ мають тільки Адмін та Логіст
    [Authorize(Roles = "Admin,Logistician")]
    public class ParcelController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ParcelController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Отримати список посилок (з фільтрами для логіста)
        [HttpGet]
        public async Task<IActionResult> Index(int? fromCityId, int? toCityId)
        {
            // Беремо всі посилки, які готові до відправки
            var parcelsQuery = _context.Parcels
                .Include(p => p.Category)
                .Include(p => p.SenderBranch).ThenInclude(b => b.City)
                .Include(p => p.ReceiverBranch).ThenInclude(b => b.City)
                .Where(p => p.Status == "Очікує відправки");

            // Фільтр "Звідки" (Поточне місцезнаходження посилок)
            if (fromCityId.HasValue)
            {
                parcelsQuery = parcelsQuery.Where(p => p.SenderBranch.CityId == fromCityId.Value);
            }

            // Фільтр "Куди" (Місто призначення)
            if (toCityId.HasValue)
            {
                parcelsQuery = parcelsQuery.Where(p => p.ReceiverBranch.CityId == toCityId.Value);
            }

            var parcels = await parcelsQuery.OrderBy(p => p.Id).ToListAsync();

            // Аналітика для логіста: рахуємо загальну вагу та кількість вибраних посилок
            ViewBag.TotalWeight = parcels.Sum(p => p.Weight);
            ViewBag.TotalCount = parcels.Count;

            // Завантажуємо міста для випадаючих списків фільтрів
            var cities = await _context.Cities.OrderBy(c => c.Name).ToListAsync();
            ViewBag.FromCities = new SelectList(cities, "Id", "Name", fromCityId);
            ViewBag.ToCities = new SelectList(cities, "Id", "Name", toCityId);

            return View(parcels);
        }

        // GET: Сторінка створення нової посилки
        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(_context.CargoCategories, "Id", "Name");

            var branches = _context.Branches.Include(b => b.City).Select(b => new
            {
                Id = b.Id,
                DisplayName = $"{b.City.Name} - Відділення №{b.Number} ({b.Address})"
            }).ToList();

            ViewBag.Branches = new SelectList(branches, "Id", "DisplayName");

            return View();
        }

        // POST: Збереження нової посилки в базу
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Parcel parcel)
        {
            var random = new Random();
            parcel.Barcode = $"TTN-{random.Next(100000, 999999)}";
            parcel.Status = "Очікує відправки";

            _context.Parcels.Add(parcel);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Посилку успішно створено! ТТН: {parcel.Barcode}";
            return RedirectToAction(nameof(Index));
        }

        // Метод для автоматичного створення 10 тестових посилок
        [HttpPost]
        public async Task<IActionResult> GenerateTestData()
        {
            var categories = await _context.CargoCategories.ToListAsync();
            var branches = await _context.Branches.ToListAsync();
            var random = new Random();

            if (!categories.Any() || branches.Count < 2)
            {
                TempData["Error"] = "Спочатку додайте категорії та хоча б 2 відділення!";
                return RedirectToAction(nameof(Index));
            }

            for (int i = 0; i < 10; i++)
            {
                var sender = branches[random.Next(branches.Count)];
                var receiver = branches[random.Next(branches.Count)];
                while (receiver.Id == sender.Id) receiver = branches[random.Next(branches.Count)];

                var parcel = new Parcel
                {
                    Barcode = $"TTN-{random.Next(100000, 999999)}",
                    Weight = (decimal)Math.Round(random.NextDouble() * 30 + 0.5, 1),
                    CategoryId = categories[random.Next(categories.Count)].Id,
                    SenderBranchId = sender.Id,
                    ReceiverBranchId = receiver.Id,
                    Status = "Очікує відправки"
                };
                _context.Parcels.Add(parcel);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}