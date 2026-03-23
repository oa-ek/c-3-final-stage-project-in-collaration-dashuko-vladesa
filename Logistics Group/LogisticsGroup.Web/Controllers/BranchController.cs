using LogisticsGroup.Domain.Entities;
using LogisticsGroup.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LogisticsGroup.Web.Controllers
{
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
            ViewBag.CityList = cityList;

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
            ViewBag.CityList = cityList;

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
            ViewBag.CityList = cityList;

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
            ViewBag.CityList = cityList;

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
    }
}