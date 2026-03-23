using LogisticsGroup.Domain.Entities;
using LogisticsGroup.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsGroup.Web.Controllers
{
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
    }
}