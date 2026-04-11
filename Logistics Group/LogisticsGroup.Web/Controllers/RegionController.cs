using LogisticsGroup.Domain.Entities;
using LogisticsGroup.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClosedXML.Excel;

namespace LogisticsGroup.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RegionController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public RegionController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        
        public IActionResult Index()
        {
            var regionList = _unitOfWork.Region.GetAll();
            return View(regionList);
        }

        
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Region obj)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Region.Add(obj);
                _unitOfWork.Save();
                return RedirectToAction("Index");
            }
            return View(obj);
        }

        
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0) return NotFound();
            var regionFromDb = _unitOfWork.Region.Get(u => u.Id == id);
            if (regionFromDb == null) return NotFound();
            return View(regionFromDb);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Region obj)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Region.Update(obj);
                _unitOfWork.Save();
                return RedirectToAction("Index");
            }
            return View(obj);
        }

        
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0) return NotFound();
            var regionFromDb = _unitOfWork.Region.Get(u => u.Id == id);
            if (regionFromDb == null) return NotFound();
            return View(regionFromDb);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int? id)
        {
            var obj = _unitOfWork.Region.Get(u => u.Id == id);
            if (obj == null) return NotFound();
            _unitOfWork.Region.Remove(obj);
            _unitOfWork.Save();
            return RedirectToAction("Index");
        }
        public IActionResult ExportToExcel()
        {
            var regions = _unitOfWork.Region.GetAll().ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Області");
                var currentRow = 1;

                worksheet.Cell(currentRow, 1).Value = "ID (Не змінювати)";
                worksheet.Cell(currentRow, 2).Value = "Назва області";
                worksheet.Row(1).Style.Font.Bold = true;

                foreach (var region in regions)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = region.Id;
                    worksheet.Cell(currentRow, 2).Value = region.Name;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Regions_List.xlsx");
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
                            var region = new Region
                            {
                                Name = row.Cell(2).GetString()
                            };
                            _unitOfWork.Region.Add(region);
                        }
                        _unitOfWork.Save();
                    }
                }
                TempData["success"] = "Області успішно імпортовано!";
            }
            return RedirectToAction("Index");
        }
    }
}