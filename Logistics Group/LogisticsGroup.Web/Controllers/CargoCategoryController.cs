using LogisticsGroup.Domain.Entities;
using LogisticsGroup.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClosedXML.Excel;
using System.IO;

namespace LogisticsGroup.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CargoCategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CargoCategoryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

       
        public IActionResult Index()
        {
            var categoryList = _unitOfWork.CargoCategory.GetAll();
            return View(categoryList);
        }

        
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CargoCategory obj)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.CargoCategory.Add(obj);
                _unitOfWork.Save();
                return RedirectToAction("Index");
            }
            return View(obj);
        }

        
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0) return NotFound();
            var categoryFromDb = _unitOfWork.CargoCategory.Get(u => u.Id == id);
            if (categoryFromDb == null) return NotFound();
            return View(categoryFromDb);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(CargoCategory obj)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.CargoCategory.Update(obj);
                _unitOfWork.Save();
                return RedirectToAction("Index");
            }
            return View(obj);
        }

       
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0) return NotFound();
            var categoryFromDb = _unitOfWork.CargoCategory.Get(u => u.Id == id);
            if (categoryFromDb == null) return NotFound();
            return View(categoryFromDb);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int? id)
        {
            var obj = _unitOfWork.CargoCategory.Get(u => u.Id == id);
            if (obj == null) return NotFound();
            _unitOfWork.CargoCategory.Remove(obj);
            _unitOfWork.Save();
            return RedirectToAction("Index");
        }
        public IActionResult ExportToExcel()
        {
            var categories = _unitOfWork.CargoCategory.GetAll().ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Категорії");
                var currentRow = 1;

                worksheet.Cell(currentRow, 1).Value = "ID (Не змінювати)";
                worksheet.Cell(currentRow, 2).Value = "Назва категорії";
                worksheet.Cell(currentRow, 3).Value = "Мін. вага (кг)";
                worksheet.Cell(currentRow, 4).Value = "Макс. вага (кг)";

                worksheet.Row(1).Style.Font.Bold = true;

                foreach (var cat in categories)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = cat.Id;
                    worksheet.Cell(currentRow, 2).Value = cat.Name;
                    worksheet.Cell(currentRow, 3).Value = cat.MinWeight;
                    worksheet.Cell(currentRow, 4).Value = cat.MaxWeight;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "CargoCategories_List.xlsx");
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ImportFromExcel(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                using (var stream = new MemoryStream())
                {
                    file.CopyTo(stream);
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

                        foreach (var row in rows)
                        {
                            string minRaw = row.Cell(3).GetString().Replace(" ", "").Replace(",", ".");
                            string maxRaw = row.Cell(4).GetString().Replace(" ", "").Replace(",", ".");

                            decimal.TryParse(minRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal minWeight);
                            decimal.TryParse(maxRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal maxWeight);

                            var category = new CargoCategory
                            {
                                Name = row.Cell(2).GetString(),
                                MinWeight = minWeight,
                                MaxWeight = maxWeight
                            };

                            _unitOfWork.CargoCategory.Add(category);
                        }
                        _unitOfWork.Save();
                    }
                }
                TempData["success"] = "Категорії вантажів успішно імпортовано з Excel!";
            }
            else
            {
                TempData["error"] = "Будь ласка, оберіть файл для імпорту.";
            }

            return RedirectToAction("Index");
        }
    }
}