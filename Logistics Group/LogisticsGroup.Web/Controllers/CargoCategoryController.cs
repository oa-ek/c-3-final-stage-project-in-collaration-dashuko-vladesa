using LogisticsGroup.Domain.Entities;
using LogisticsGroup.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsGroup.Web.Controllers
{
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
    }
}