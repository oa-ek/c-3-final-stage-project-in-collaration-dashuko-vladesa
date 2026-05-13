using LogisticsGroup.Domain.Entities;
using LogisticsGroup.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using ClosedXML.Excel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using LogisticsGroup.Web.Services; // Додано для доступу до сервісів

namespace LogisticsGroup.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BranchController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGeocodingApiService _nominatimService; // Підключаємо наш сервіс карт

        // Додаємо сервіс в конструктор
        public BranchController(IUnitOfWork unitOfWork, IGeocodingApiService nominatimService)
        {
            _unitOfWork = unitOfWork;
            _nominatimService = nominatimService;
        }

        // --- ДОПОМІЖНІ МЕТОДИ ---

        private string BuildAddressForApi(string cityName, string streetAddress)
        {
            var city = string.IsNullOrWhiteSpace(cityName) ? "" : cityName.Trim();
            var street = string.IsNullOrWhiteSpace(streetAddress) ? "" : streetAddress;

            street = street.Replace("вул.", " ")
                           .Replace("вулиця", " ")
                           .Replace("буд.", " ")
                           .Replace("б.", " ")
                           .Replace("просп.", " ")
                           .Replace("проспект", " ")
                           .Replace("пров.", " ");

            street = System.Text.RegularExpressions.Regex.Replace(street, @"\s+", " ").Trim();

            var query = $"{city}, {street}, Україна";
            query = query.Replace(" ,", ",");

            return query;
        }

        // Тепер метод чистий і красивий, він просто передає роботу нашому сервісу з кешем!
        private async Task<(double? Lat, double? Lng)> GetCoordinatesFromAddress(string address)
        {
            return await _nominatimService.GetCoordinatesAsync(address);
        }

        // --- ОСНОВНІ МЕТОДИ КОНТРОЛЕРА ---

        public IActionResult Index()
        {
            var branchList = _unitOfWork.Branch.GetAll().ToList();
            var cityList = _unitOfWork.City.GetAll().ToList();

            foreach (var branch in branchList)
            {
                branch.City = cityList.FirstOrDefault(c => c.Id == branch.CityId);
            }

            return View(branchList);
        }

        public IActionResult Create()
        {
            IEnumerable<SelectListItem> cityList = _unitOfWork.City.GetAll().Select(u => new SelectListItem
            {
                Text = u.Name,
                Value = u.Id.ToString()
            });

            ViewBag.CityId = cityList;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Branch obj)
        {
            ModelState.Remove("City");

            if (ModelState.IsValid)
            {
                var city = _unitOfWork.City.Get(u => u.Id == obj.CityId);
                var cityName = city != null ? city.Name : "";

                var fullAddress = BuildAddressForApi(cityName, obj.Address);

                var coords = await GetCoordinatesFromAddress(fullAddress);
                obj.Latitude = coords.Lat;
                obj.Longitude = coords.Lng;

                _unitOfWork.Branch.Add(obj);
                _unitOfWork.Save();
                TempData["success"] = "Відділення успішно створено!";
                return RedirectToAction("Index");
            }

            IEnumerable<SelectListItem> cityList = _unitOfWork.City.GetAll().Select(u => new SelectListItem
            {
                Text = u.Name,
                Value = u.Id.ToString()
            });

            ViewBag.CityId = cityList;

            return View(obj);
        }

        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0) return NotFound();
            var branchFromDb = _unitOfWork.Branch.Get(u => u.Id == id);
            if (branchFromDb == null) return NotFound();

            IEnumerable<SelectListItem> cityList = _unitOfWork.City.GetAll().Select(u => new SelectListItem
            {
                Text = u.Name,
                Value = u.Id.ToString()
            });

            ViewBag.CityId = cityList;

            return View(branchFromDb);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Branch obj)
        {
            ModelState.Remove("City");

            if (ModelState.IsValid)
            {
                var city = _unitOfWork.City.Get(u => u.Id == obj.CityId);
                var cityName = city != null ? city.Name : "";

                var fullAddress = BuildAddressForApi(cityName, obj.Address);

                var coords = await GetCoordinatesFromAddress(fullAddress);
                obj.Latitude = coords.Lat;
                obj.Longitude = coords.Lng;

                _unitOfWork.Branch.Update(obj);
                _unitOfWork.Save();
                TempData["success"] = "Відділення успішно оновлено!";
                return RedirectToAction("Index");
            }

            IEnumerable<SelectListItem> cityList = _unitOfWork.City.GetAll().Select(u => new SelectListItem
            {
                Text = u.Name,
                Value = u.Id.ToString()
            });

            ViewBag.CityId = cityList;

            return View(obj);
        }

        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0) return NotFound();
            var branchFromDb = _unitOfWork.Branch.Get(u => u.Id == id);
            if (branchFromDb == null) return NotFound();

            branchFromDb.City = _unitOfWork.City.Get(u => u.Id == branchFromDb.CityId);

            return View(branchFromDb);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int? id)
        {
            var obj = _unitOfWork.Branch.Get(u => u.Id == id);
            if (obj == null) return NotFound();
            _unitOfWork.Branch.Remove(obj);
            _unitOfWork.Save();
            TempData["success"] = "Відділення успішно видалено!";
            return RedirectToAction("Index");
        }

        public IActionResult ExportToExcel()
        {
            var branches = _unitOfWork.Branch.GetAll().ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Відділення та Поштомати");
                var currentRow = 1;

                worksheet.Cell(currentRow, 1).Value = "ID (Не змінювати)";
                worksheet.Cell(currentRow, 2).Value = "Номер";
                worksheet.Cell(currentRow, 3).Value = "Адреса";
                worksheet.Cell(currentRow, 4).Value = "Тип (Відділення/Поштомат)";
                worksheet.Cell(currentRow, 5).Value = "Години роботи";
                worksheet.Cell(currentRow, 6).Value = "Максимальна вага";
                worksheet.Cell(currentRow, 7).Value = "ID Міста (CityId)";

                worksheet.Row(1).Style.Font.Bold = true;

                foreach (var branch in branches)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = branch.Id;
                    worksheet.Cell(currentRow, 2).Value = branch.Number;
                    worksheet.Cell(currentRow, 3).Value = branch.Address;
                    worksheet.Cell(currentRow, 4).Value = branch.Type;
                    worksheet.Cell(currentRow, 5).Value = branch.WorkingHours;
                    worksheet.Cell(currentRow, 6).Value = branch.MaxWeight;
                    worksheet.Cell(currentRow, 7).Value = branch.CityId;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Branches_List.xlsx");
                }
            }
        }

        // --- ВИПРАВЛЕНИЙ ІМПОРТ ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ImportFromExcel(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                int addedCount = 0;
                int skippedCount = 0;

                using (var stream = new MemoryStream())
                {
                    file.CopyTo(stream);
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

                        foreach (var row in rows)
                        {
                            decimal? parsedMaxWeight = null;
                            if (decimal.TryParse(row.Cell(6).GetString(), out decimal maxWeight))
                            {
                                parsedMaxWeight = maxWeight;
                            }

                            if (int.TryParse(row.Cell(7).GetString(), out int excelCityId))
                            {
                                var cityExists = _unitOfWork.City.Get(u => u.Id == excelCityId);

                                if (cityExists == null)
                                {
                                    skippedCount++;
                                    continue;
                                }

                                var branch = new Branch
                                {
                                    Number = row.Cell(2).GetString(),
                                    Address = row.Cell(3).GetString(),
                                    Type = row.Cell(4).GetString(),
                                    WorkingHours = row.Cell(5).GetString(),
                                    MaxWeight = parsedMaxWeight,
                                    CityId = excelCityId
                                };

                                _unitOfWork.Branch.Add(branch);
                                addedCount++;
                            }
                            else
                            {
                                skippedCount++;
                            }
                        }
                        _unitOfWork.Save();
                    }
                }

                if (skippedCount > 0)
                {
                    TempData["error"] = $"Додано відділень: {addedCount}. Пропущено: {skippedCount} (у файлі вказані неіснуючі ID міст).";
                }
                else
                {
                    TempData["success"] = $"Успішно імпортовано {addedCount} відділень! Не забудьте синхронізувати координати на карті.";
                }
            }
            return RedirectToAction("Index");
        }
        [AllowAnonymous]
        public IActionResult Map()
        {
            var branchList = _unitOfWork.Branch.GetAll().ToList();
            var cityList = _unitOfWork.City.GetAll().ToList();

            foreach (var branch in branchList)
            {
                branch.City = cityList.FirstOrDefault(c => c.Id == branch.CityId);
            }

            return View(branchList);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncCoordinates()
        {
            var branchList = _unitOfWork.Branch.GetAll().ToList();
            var cityList = _unitOfWork.City.GetAll().ToList();

            int exactMatches = 0;
            int cityFallbackMatches = 0;
            List<string> completelyFailed = new List<string>();

            var branchesToUpdate = branchList.Where(b => b.Latitude == null || b.Longitude == null).ToList();

            foreach (var branch in branchesToUpdate)
            {
                var city = cityList.FirstOrDefault(c => c.Id == branch.CityId);
                var cityName = city != null ? city.Name : "";

                var exactAddress = $"{cityName}, {branch.Address}, Україна";
                var coords = await GetCoordinatesFromAddress(exactAddress);

                if (coords.Lat.HasValue && coords.Lng.HasValue)
                {
                    branch.Latitude = coords.Lat;
                    branch.Longitude = coords.Lng;
                    exactMatches++;
                }
                else
                {
                    await Task.Delay(1500); // Затримка залишається, щоб не перевантажувати зовнішній API, якщо кеш порожній

                    var cityOnlyAddress = $"{cityName}, Україна";
                    coords = await GetCoordinatesFromAddress(cityOnlyAddress);

                    if (coords.Lat.HasValue && coords.Lng.HasValue)
                    {
                        branch.Latitude = coords.Lat;
                        branch.Longitude = coords.Lng;
                        cityFallbackMatches++;
                    }
                    else
                    {
                        completelyFailed.Add(exactAddress);
                    }
                }

                _unitOfWork.Branch.Update(branch);
                await Task.Delay(1500);
            }

            if (exactMatches > 0 || cityFallbackMatches > 0)
            {
                _unitOfWork.Save();
                TempData["success"] = $"Синхронізація завершена! Точних адрес знайдено: {exactMatches}. Приблизних (по центру міста): {cityFallbackMatches}.";
            }

            if (completelyFailed.Any())
            {
                var examples = string.Join(" | ", completelyFailed.Take(3));
                TempData["error"] = $"Не вдалося знайти навіть міст для {completelyFailed.Count} відділень. Перевірте назви населених пунктів: {examples}";
            }
            else if (TempData["error"] != null)
            {
                TempData["error"] = null;
            }

            return RedirectToAction("Map");
        }
    }
}