using Microsoft.AspNetCore.Mvc;
using UserManagementApp.Models;
using System.Linq;

namespace UserManagementApp.Controllers
{
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Dashboard()
        {
            // ✅ SESSION CHECK (IMPORTANT)
            if (HttpContext.Session.GetString("Username") == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var username = HttpContext.Session.GetString("Username");

            ViewBag.Username = username;
            ViewBag.TotalUsers = _context.Users.Count();
            ViewBag.TotalAdmins = _context.Users.Count(u => u.RoleId == 1);
            ViewBag.TotalUsersOnly = _context.Users.Count(u => u.RoleId == 2);

            return View();
        }

        public IActionResult Profile()
        {
            // ✅ SESSION CHECK
            if (HttpContext.Session.GetString("Username") == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var userId = HttpContext.Session.GetInt32("UserId");

            var user = _context.Users.FirstOrDefault(u => u.UserId == userId);

            return View(user);
        }
    }
}