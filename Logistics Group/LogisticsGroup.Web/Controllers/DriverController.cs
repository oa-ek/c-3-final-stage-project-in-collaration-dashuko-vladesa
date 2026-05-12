using LogisticsGroup.Domain.Entities;
using LogisticsGroup.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ClosedXML.Excel;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Text.RegularExpressions;

namespace LogisticsGroup.Web.Controllers
{
    public class DriverController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<IdentityUser> _userManager;

        public DriverController(IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            var driversList = _unitOfWork.Driver.GetAll();
            return View(driversList);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Driver obj)
        {
            if (ModelState.IsValid)
            {
                obj.Status = "Вільний";
                _unitOfWork.Driver.Add(obj);
                _unitOfWork.Save();

                // СТАБІЛЬНА ПОШТА (без рандому)
                string email = GenerateDriverEmail(obj.FullName);
                string password = $"Driver_{new Random().Next(1000, 9999)}!";

                var user = new IdentityUser { UserName = email, Email = email };
                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Driver");
                    TempData["SuccessMessage"] = $"Водія додано! 🔑 Логін: {email} | Пароль: {password}";
                }
                else
                {
                    TempData["ErrorMessage"] = "Помилка Identity: " + string.Join(", ", result.Errors.Select(e => e.Description));
                }

                return RedirectToAction("Index");
            }
            return View(obj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var driver = _unitOfWork.Driver.Get(u => u.Id == id);
            if (driver == null) return NotFound();

            // Тепер ця функція видасть ТОЙ САМИЙ email, що і при створенні
            var email = GenerateDriverEmail(driver.FullName);
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                TempData["ErrorMessage"] = $"Користувача {email} не знайдено. Можливо, він був створений зі старим рандомним логіном.";
                return RedirectToAction(nameof(Index));
            }

            string newPassword = $"Reset_{new Random().Next(1000, 9999)}!";

            await _userManager.RemovePasswordAsync(user);
            var result = await _userManager.AddPasswordAsync(user, newPassword);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Пароль для {driver.FullName} успішно змінено! 🔑 Новий пароль: {newPassword}";
            }
            else
            {
                TempData["ErrorMessage"] = "Помилка скидання: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Index));
        }

        // СТАБІЛЬНА ГЕНЕРАЦІЯ БЕЗ РАНДОМУ
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

        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0) return NotFound();
            var driverFromDb = _unitOfWork.Driver.Get(u => u.Id == id);
            if (driverFromDb == null) return NotFound();
            return View(driverFromDb);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Driver obj)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Driver.Update(obj);
                _unitOfWork.Save();
                return RedirectToAction("Index");
            }
            return View(obj);
        }

        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0) return NotFound();
            var driverFromDb = _unitOfWork.Driver.Get(u => u.Id == id);
            if (driverFromDb == null) return NotFound();
            return View(driverFromDb);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int? id)
        {
            var obj = _unitOfWork.Driver.Get(u => u.Id == id);
            if (obj == null) return NotFound();
            _unitOfWork.Driver.Remove(obj);
            _unitOfWork.Save();
            TempData["SuccessMessage"] = "Водія успішно видалено.";
            return RedirectToAction("Index");
        }

        public IActionResult ExportToExcel()
        {
            var drivers = _unitOfWork.Driver.GetAll();
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Водії");
                worksheet.Cell(1, 1).Value = "ID";
                worksheet.Cell(1, 2).Value = "ПІБ";
                worksheet.Cell(1, 3).Value = "Телефон";
                worksheet.Cell(1, 4).Value = "Статус";
                worksheet.Row(1).Style.Font.Bold = true;

                int row = 2;
                foreach (var d in drivers)
                {
                    worksheet.Cell(row, 1).Value = d.Id;
                    worksheet.Cell(row, 2).Value = d.FullName;
                    worksheet.Cell(row, 3).Value = d.Phone;
                    worksheet.Cell(row, 4).Value = d.Status;
                    row++;
                }
                worksheet.Columns().AdjustToContents();
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Drivers.xlsx");
                }
            }
        }

        [HttpPost]
        public IActionResult ImportFromExcel(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                using (var stream = new MemoryStream())
                {
                    file.CopyTo(stream);
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var rows = workbook.Worksheet(1).RangeUsed().RowsUsed().Skip(1);
                        foreach (var row in rows)
                        {
                            _unitOfWork.Driver.Add(new Driver
                            {
                                FullName = row.Cell(2).GetString(),
                                Phone = row.Cell(3).GetString(),
                                Status = "Вільний"
                            });
                        }
                        _unitOfWork.Save();
                    }
                }
                TempData["SuccessMessage"] = "Дані успішно імпортовано!";
            }
            return RedirectToAction("Index");
        }
    }
}