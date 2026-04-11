using LogisticsGroup.Domain.Entities;
using LogisticsGroup.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClosedXML.Excel;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace LogisticsGroup.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DriverController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public DriverController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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
        public IActionResult Create(Driver obj)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Driver.Add(obj);
                _unitOfWork.Save();
                return RedirectToAction("Index");
            }
            return View(obj);
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
            return RedirectToAction("Index");
        }

        public IActionResult ExportToExcel()
        {
            var drivers = _unitOfWork.Driver.GetAll();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Водії");
                var currentRow = 1;

                worksheet.Cell(currentRow, 1).Value = "ID";
                worksheet.Cell(currentRow, 2).Value = "ПІБ";
                worksheet.Cell(currentRow, 3).Value = "Телефон";
                worksheet.Cell(currentRow, 4).Value = "Номер посвідчення";
                worksheet.Cell(currentRow, 5).Value = "Статус";

                worksheet.Row(1).Style.Font.Bold = true;

                foreach (var driver in drivers)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = driver.Id;
                    worksheet.Cell(currentRow, 2).Value = driver.FullName;
                    worksheet.Cell(currentRow, 3).Value = driver.Phone;
                    worksheet.Cell(currentRow, 4).Value = driver.LicenseNumber;
                    worksheet.Cell(currentRow, 5).Value = driver.Status;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Drivers_List.xlsx");
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
                            var driver = new Driver
                            {
                                FullName = row.Cell(2).GetString(),
                                Phone = row.Cell(3).GetString(),
                                LicenseNumber = row.Cell(4).GetString(),
                                Status = row.Cell(5).GetString()
                            };

                            _unitOfWork.Driver.Add(driver);
                        }
                        _unitOfWork.Save();
                    }
                }
                TempData["SuccessMessage"] = "Водіїв успішно імпортовано!";
            }
            return RedirectToAction("Index");
        }
    }
}