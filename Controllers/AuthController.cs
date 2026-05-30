using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using UserManagementApp.Helpers;
using UserManagementApp.Models;


namespace UserManagementApp.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        // ---------------- REGISTER (GET) ----------------
        public IActionResult Register()
        {
            return View();
        }

        // ---------------- REGISTER (POST) ----------------
        [HttpPost]
        
        public IActionResult Register(User user)
        {
            if (!ModelState.IsValid)
                return View(user);

            // 🔐 Password match
            if (user.Password != Request.Form["Confirmpassword"])
            {
                ViewBag.Error = "Password does not match ❌";
                return View(user);
            }

            // 📅 Future DOB check
            if (user.Dob > DateTime.Today)
            {
                ViewBag.Error = "Future date not allowed ❌";
                return View(user);
            }

            // 📧 Email unique
            if (_context.Users.Any(u => u.Email == user.Email))
            {
                ViewBag.Error = "Email already exists ❌";
                return View(user);
            }

            // 📱 Mobile unique
            if (_context.Users.Any(u => u.Mobile == user.Mobile))
            {
                ViewBag.Error = "Mobile already exists ❌";
                return View(user);
            }

            // 🎭 Role
            user.RoleId = Request.Form["Role"] == "admin" ? 1 : 2;

            // 🔐 Hash password
            var hasher = new PasswordHasher<User>();
            user.Password = hasher.HashPassword(user, user.Password);

            // 📅 Dates
            user.CreatedDate = DateTime.UtcNow;
            user.Dob = DateTime.SpecifyKind(user.Dob, DateTimeKind.Utc);

            _context.Users.Add(user);
            _context.SaveChanges();

            TempData["Success"] = "Registration Successful ✅ Please Login";

            return RedirectToAction("Login");
        }

        // ---------------- LOGIN (GET) ----------------
        public IActionResult Login()
        {
            return View(new LoginModel());
        }

        // ---------------- LOGIN (POST) ----------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = _context.Users.FirstOrDefault(u => u.Username == model.Username);

            if (user == null)
            {
                ViewBag.Error = "Invalid Username ❌";
                return View(model);
            }

            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(user, user.Password, model.Password);

            if (result != PasswordVerificationResult.Success)
            {
                ViewBag.Error = "Invalid Password ❌";
                return View(model);
            }

            // ✅ SESSION LOGIN
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetInt32("RoleId", user.RoleId);
            HttpContext.Session.SetInt32("UserId", user.UserId);

            // 🔐 JWT GENERATION
            var jwtHelper = new JwtHelper();
            var token = JwtHelper.GenerateToken(user.Username, user.RoleId);

            HttpContext.Session.SetString("JWT", token);

            TempData["Success"] = "Login Successful ✅";

            return RedirectToAction("Dashboard", "Dashboard");
        }

        // ---------------- LOGOUT ----------------
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            Response.Cookies.Delete(".AspNetCore.Session");

            return RedirectToAction("Login");
        }
    }
}