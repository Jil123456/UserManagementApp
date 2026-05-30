using Microsoft.AspNetCore.Mvc;
using UserManagementApp.Models;
using System.Linq;

namespace UserManagementApp.Controllers
{
    public class UserController : Controller
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult UserList()
        {
            if (HttpContext.Session.GetString("Username") == null)
                return RedirectToAction("Login", "Auth");

            if (HttpContext.Session.GetInt32("RoleId") != 1)
                return RedirectToAction("Dashboard", "Dashboard");

            var users = _context.Users.ToList();
            return View(users);
        }

        // EDIT (GET)
        public IActionResult Edit(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserId == id);
            if (user == null) return NotFound();

            return View(user);
        }

        // EDIT (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(User updatedUser)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserId == updatedUser.UserId);

            if (user == null) return NotFound();

            user.Fullname = updatedUser.Fullname;
            user.Username = updatedUser.Username;
            user.Email = updatedUser.Email;
            user.Mobile = updatedUser.Mobile;

            _context.SaveChanges();

            TempData["Success"] = "User updated successfully ✅";

            return RedirectToAction("UserList");
        }

        // DELETE (FIXED)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var user = _context.Users.Find(id);

            if (user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
            }

            TempData["Success"] = "User deleted successfully ✅";

            return RedirectToAction("UserList");
        }
    }
}