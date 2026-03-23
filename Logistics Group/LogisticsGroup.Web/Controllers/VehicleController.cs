using LogisticsGroup.Domain.Entities;
using LogisticsGroup.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsGroup.Web.Controllers
{
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
    }
}