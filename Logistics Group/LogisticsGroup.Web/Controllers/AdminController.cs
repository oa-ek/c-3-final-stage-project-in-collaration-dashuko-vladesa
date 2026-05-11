using LogisticsGroup.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsGroup.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        public AdminController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        // GET: Список усього персоналу
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userViewModels = new List<UserViewModel>();

            var currentUser = await _userManager.GetUserAsync(User); // Поточний адмін

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                // Перевіряємо, чи дата розблокування більша за поточний час
                var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? "Невідомо",
                    Role = roles.FirstOrDefault() ?? "Без ролі",
                    IsLockedOut = isLocked,
                    IsCurrentUser = (currentUser != null && user.Id == currentUser.Id)
                });
            }

            return View(userViewModels);
        }

        // POST: Блокування / Розблокування працівника
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && user.Id == currentUser.Id)
            {
                TempData["ErrorMessage"] = "Ви не можете звільнити самі себе!";
                return RedirectToAction(nameof(Index));
            }

            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
            {
                // Розблокувати (скидаємо дату)
                await _userManager.SetLockoutEndDateAsync(user, null);
                TempData["SuccessMessage"] = $"Доступ для {user.Email} відновлено!";
            }
            else
            {
                // Заблокувати (ставимо дату на 2999 рік)
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                TempData["SuccessMessage"] = $"Співробітника {user.Email} звільнено (акаунт заблоковано)!";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Сторінка створення
        [HttpGet]
        public IActionResult CreateStaff()
        {
            return View();
        }

        // POST: Створення працівника
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStaff(RegisterStaffViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new IdentityUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    EmailConfirmed = true,
                    LockoutEnabled = true // Обов'язково вмикаємо можливість блокування
                };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, model.Role);

                    TempData["SuccessMessage"] = $"Співробітника {model.Email} успішно створено з роллю {model.Role}!";
                    return RedirectToAction("Index"); // Після створення кидаємо на таблицю всіх працівників
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }
    }
}