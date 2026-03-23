using LogisticsGroup.Domain.Entities;
using LogisticsGroup.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsGroup.Web.Controllers
{
    public class DriverController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public DriverController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // Логіст дивиться список усіх водіїв компанії
        public IActionResult Index()
        {
            var driversList = _unitOfWork.Driver.GetAll();
            return View(driversList);
        }

        // Логіст наймає нового водія
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

        // Логіст змінює дані водія (наприклад, номер телефону)
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

        // Логіст звільняє водія
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
    }
}