using LogisticsGroup.Domain.Entities;
using LogisticsGroup.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using ClosedXML.Excel;

namespace LogisticsGroup.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BranchController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public BranchController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

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

            // ВИПРАВЛЕНО: CityId замість CityList
            ViewBag.CityId = cityList;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Branch obj)
        {
            ModelState.Remove("City");

            if (ModelState.IsValid)
            {
                _unitOfWork.Branch.Add(obj);
                _unitOfWork.Save();
                return RedirectToAction("Index");
            }

            IEnumerable<SelectListItem> cityList = _unitOfWork.City.GetAll().Select(u => new SelectListItem
            {
                Text = u.Name,
                Value = u.Id.ToString()
            });

            // ВИПРАВЛЕНО: CityId замість CityList
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

            // ВИПРАВЛЕНО: CityId замість CityList
            ViewBag.CityId = cityList;

            return View(branchFromDb);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Branch obj)
        {
            ModelState.Remove("City");

            if (ModelState.IsValid)
            {
                _unitOfWork.Branch.Update(obj);
                _unitOfWork.Save();
                return RedirectToAction("Index");
            }

            IEnumerable<SelectListItem> cityList = _unitOfWork.City.GetAll().Select(u => new SelectListItem
            {
                Text = u.Name,
                Value = u.Id.ToString()
            });

            // ВИПРАВЛЕНО: CityId замість CityList
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
                            decimal? parsedMaxWeight = null;
                            if (decimal.TryParse(row.Cell(6).GetString(), out decimal maxWeight))
                            {
                                parsedMaxWeight = maxWeight;
                            }

                            int.TryParse(row.Cell(7).GetString(), out int cityId);

                            var branch = new Branch
                            {
                                Number = row.Cell(2).GetString(),
                                Address = row.Cell(3).GetString(),
                                Type = row.Cell(4).GetString(),
                                WorkingHours = row.Cell(5).GetString(), 
                                MaxWeight = parsedMaxWeight,
                                CityId = cityId
                            };

                            _unitOfWork.Branch.Add(branch);
                        }
                        _unitOfWork.Save();
                    }
                }
                TempData["success"] = "Відділення та поштомати успішно імпортовано!";
            }
            return RedirectToAction("Index");
        }
    }
}