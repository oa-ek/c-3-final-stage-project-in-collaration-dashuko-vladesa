using LogisticsGroup.Domain.Entities;
using LogisticsGroup.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

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
    }
}