using ClosedXML.Excel;
using LogisticsGroup.Domain.Entities;
using LogisticsGroup.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ClosedXML.Excel;

namespace LogisticsGroup.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CityController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CityController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        
        public IActionResult Index()
        {
            
            var cityList = _unitOfWork.City.GetAll().ToList();

           
            var regionList = _unitOfWork.Region.GetAll().ToList();

            
            foreach (var city in cityList)
            {
                city.Region = regionList.FirstOrDefault(r => r.Id == city.RegionId);
            }

            return View(cityList);
        }

        public IActionResult Create()
        {
            IEnumerable<SelectListItem> regionList = _unitOfWork.Region.GetAll().Select(u => new SelectListItem
            {
                Text = u.Name,
                Value = u.Id.ToString()
            });
            ViewBag.RegionList = regionList;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(City obj)
        {
            ModelState.Remove("Region");
            ModelState.Remove("Branches");

            if (ModelState.IsValid)
            {
                _unitOfWork.City.Add(obj);
                _unitOfWork.Save();
                return RedirectToAction("Index");
            }

            IEnumerable<SelectListItem> regionList = _unitOfWork.Region.GetAll().Select(u => new SelectListItem
            {
                Text = u.Name,
                Value = u.Id.ToString()
            });
            ViewBag.RegionList = regionList;

            return View(obj);
        }

        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0) return NotFound();
            var cityFromDb = _unitOfWork.City.Get(u => u.Id == id);
            if (cityFromDb == null) return NotFound();

            IEnumerable<SelectListItem> regionList = _unitOfWork.Region.GetAll().Select(u => new SelectListItem
            {
                Text = u.Name,
                Value = u.Id.ToString()
            });
            ViewBag.RegionList = regionList;

            return View(cityFromDb);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(City obj)
        {
            ModelState.Remove("Region");
            ModelState.Remove("Branches");

            if (ModelState.IsValid)
            {
                _unitOfWork.City.Update(obj);
                _unitOfWork.Save();
                return RedirectToAction("Index");
            }

            IEnumerable<SelectListItem> regionList = _unitOfWork.Region.GetAll().Select(u => new SelectListItem
            {
                Text = u.Name,
                Value = u.Id.ToString()
            });
            ViewBag.RegionList = regionList;

            return View(obj);
        }

        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0) return NotFound();
            var cityFromDb = _unitOfWork.City.Get(u => u.Id == id);
            if (cityFromDb == null) return NotFound();

            
            cityFromDb.Region = _unitOfWork.Region.Get(u => u.Id == cityFromDb.RegionId);

            return View(cityFromDb);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int? id)
        {
            var obj = _unitOfWork.City.Get(u => u.Id == id);
            if (obj == null) return NotFound();
            _unitOfWork.City.Remove(obj);
            _unitOfWork.Save();
            return RedirectToAction("Index");
        }
        public IActionResult ExportToExcel()
        {
            var cities = _unitOfWork.City.GetAll().ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Міста");
                var currentRow = 1;

                worksheet.Cell(currentRow, 1).Value = "ID (Не змінювати)";
                worksheet.Cell(currentRow, 2).Value = "Назва міста";
                worksheet.Cell(currentRow, 3).Value = "Тип (місто/смт/село)";
                worksheet.Cell(currentRow, 4).Value = "ID Області (RegionId)";
                worksheet.Row(1).Style.Font.Bold = true;

                foreach (var city in cities)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = city.Id;
                    worksheet.Cell(currentRow, 2).Value = city.Name;
                    worksheet.Cell(currentRow, 3).Value = city.Type;
                    worksheet.Cell(currentRow, 4).Value = city.RegionId;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Cities_List.xlsx");
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
                            int.TryParse(row.Cell(4).GetString(), out int regionId);

                            var city = new City
                            {
                                Name = row.Cell(2).GetString(),
                                Type = row.Cell(3).GetString(),
                                RegionId = regionId
                            };
                            _unitOfWork.City.Add(city);
                        }
                        _unitOfWork.Save();
                    }
                }
                TempData["success"] = "Міста успішно імпортовано!";
            }
            return RedirectToAction("Index");
        }
    }
}