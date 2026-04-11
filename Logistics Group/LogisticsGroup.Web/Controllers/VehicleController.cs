using LogisticsGroup.Domain.Entities;
using LogisticsGroup.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClosedXML.Excel;

namespace LogisticsGroup.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class VehicleController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public VehicleController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        
        public IActionResult Index()
        {
            
            var vehicleList = _unitOfWork.Vehicle.GetAll();
            return View(vehicleList);
        }

        
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Vehicle obj)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Vehicle.Add(obj);
                _unitOfWork.Save();
                return RedirectToAction("Index");
            }
            return View(obj);
        }

        
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0) return NotFound();
            var vehicleFromDb = _unitOfWork.Vehicle.Get(u => u.Id == id);
            if (vehicleFromDb == null) return NotFound();
            return View(vehicleFromDb);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Vehicle obj)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Vehicle.Update(obj);
                _unitOfWork.Save();
                return RedirectToAction("Index");
            }
            return View(obj);
        }

       
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0) return NotFound();
            var vehicleFromDb = _unitOfWork.Vehicle.Get(u => u.Id == id);
            if (vehicleFromDb == null) return NotFound();
            return View(vehicleFromDb);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int? id)
        {
            var obj = _unitOfWork.Vehicle.Get(u => u.Id == id);
            if (obj == null) return NotFound();
            _unitOfWork.Vehicle.Remove(obj);
            _unitOfWork.Save();
            return RedirectToAction("Index");
        }

        public IActionResult ExportToExcel()
        {
            var vehicles = _unitOfWork.Vehicle.GetAll().ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Автомобілі");
                var currentRow = 1;

                worksheet.Cell(currentRow, 1).Value = "ID (Не змінювати)";
                worksheet.Cell(currentRow, 2).Value = "Номерний знак";
                worksheet.Cell(currentRow, 3).Value = "Марка";
                worksheet.Cell(currentRow, 4).Value = "Вантажопідйомність (т)";
                worksheet.Cell(currentRow, 5).Value = "Статус";

                worksheet.Row(1).Style.Font.Bold = true;

                foreach (var vehicle in vehicles)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = vehicle.Id;
                    worksheet.Cell(currentRow, 2).Value = vehicle.LicensePlate;
                    worksheet.Cell(currentRow, 3).Value = vehicle.Brand;
                    worksheet.Cell(currentRow, 4).Value = vehicle.Capacity;
                    worksheet.Cell(currentRow, 5).Value = vehicle.Status;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Vehicles_List.xlsx");
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
                            string capacityRaw = row.Cell(4).GetString()
                                                    .Replace("т.", "")
                                                    .Replace("т", "")
                                                    .Replace(" ", "")
                                                    .Trim()
                                                    .Replace(",", ".");

                            decimal.TryParse(capacityRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal capacity);

                            var vehicle = new Vehicle
                            {
                                Brand = row.Cell(2).GetString(),         
                                LicensePlate = row.Cell(3).GetString(),  
                                Capacity = capacity,                     
                                Status = row.Cell(5).GetString()         
                            };

                            _unitOfWork.Vehicle.Add(vehicle);
                        }
                        _unitOfWork.Save();
                    }
                }
                TempData["success"] = "Автомобілі успішно імпортовано з Excel!";
            }
            else
            {
                TempData["error"] = "Будь ласка, оберіть файл для імпорту.";
            }

            return RedirectToAction("Index");
        }
    }
}