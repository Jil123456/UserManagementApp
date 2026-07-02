using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UserManagementApp.Helpers;
using UserManagementApp.Models;
using UserManagementApp.Services;
using UserManagementApp.Constants;

namespace UserManagementApp.Controllers
{
    public class AuthController : Controller
    {
        private readonly IUserService _userService;
        private readonly IDocumentService _docService;
        private readonly IRoleService _roleService;
        private readonly IAuditLogService _auditLogService;

        public AuthController(IUserService userService, IDocumentService docService, IRoleService roleService, IAuditLogService auditLogService)
        {
            _userService = userService;
            _docService = docService;
            _roleService = roleService;
            _auditLogService = auditLogService;
        }

        // ── MATH CAPTCHA HELPER ──
        private void GenerateMathCaptcha()
        {
            var rand = new Random();
            int num1 = rand.Next(1, 20);
            int num2 = rand.Next(1, 20);

            HttpContext.Session.SetInt32("CaptchaAnswer", num1 + num2);
            ViewBag.CaptchaNum1 = num1;
            ViewBag.CaptchaNum2 = num2;
        }

        private bool ValidateMathCaptcha()
        {
            var userAnswer = Request.Form["CaptchaAnswer"].ToString();
            var correctAnswer = HttpContext.Session.GetInt32("CaptchaAnswer");

            if (string.IsNullOrEmpty(userAnswer) || correctAnswer == null)
                return false;

            if (int.TryParse(userAnswer, out int answer))
                return answer == correctAnswer.Value;

            return false;
        }

        // ────────────── REGISTER (GET) ──────────────
        public IActionResult Register()
        {
            GenerateMathCaptcha();
            ViewBag.Roles = _roleService.GetAllRoles().Where(r => r.RoleId != 3).ToList();
            return View();
        }

        // ────────────── REGISTER (POST) ──────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(User user)
        {
            // Validate math captcha
            if (!ValidateMathCaptcha())
            {
                GenerateMathCaptcha();
                ViewBag.Error = "Incorrect captcha answer! Please try again ❌";
                return View(user);
            }

            if (!ModelState.IsValid)
            {
                GenerateMathCaptcha();
                ViewBag.Roles = _roleService.GetAllRoles().Where(r => r.RoleId != 3).ToList();
                return View(user);
            }

            // Password match
            if (user.Password != Request.Form["Confirmpassword"])
            {
                GenerateMathCaptcha();
                ViewBag.Error = "Password does not match ❌";
                ViewBag.Roles = _roleService.GetAllRoles().Where(r => r.RoleId != 3).ToList();
                return View(user);
            }

            // DOB check
            if (user.Dob > DateTime.Today)
            {
                GenerateMathCaptcha();
                ViewBag.Error = "Future date not allowed ❌";
                ViewBag.Roles = _roleService.GetAllRoles().Where(r => r.RoleId != 3).ToList();
                return View(user);
            }

            // Duplicate username check
            var existingUser = _userService.GetUserByUsername(user.Username);
            if (existingUser != null)
            {
                GenerateMathCaptcha();
                ViewBag.Error = "Username already exists! Please choose a different username ❌";
                ViewBag.Roles = _roleService.GetAllRoles().Where(r => r.RoleId != 3).ToList();
                return View(user);
            }

            // Prevent SuperAdmin (3) injection.
            if (user.RoleId == 3)
            {
                user.RoleId = 2;
            }

            // Hash password
            var hasher = new PasswordHasher<User>();
            user.Password = hasher.HashPassword(user, user.Password);

            user.CreatedDate = DateTime.UtcNow;
            user.Dob = DateTime.SpecifyKind(user.Dob, DateTimeKind.Utc);

            // ✅ STATUS = APPROVED (Removed manual account verification step)
            user.Status = "Approved";

            // Save using Dapper
            _userService.AddUser(user);

            // Re-fetch to get the generated UserId
            var newUser = _userService.GetUserByUsername(user.Username);
            
            if (newUser != null)
            {
                // ✅ Auto Login
                HttpContext.Session.SetString("Username", newUser.Username);
                HttpContext.Session.SetInt32("RoleId", newUser.RoleId);
                HttpContext.Session.SetInt32("UserId", newUser.UserId);

                var token = JwtHelper.GenerateToken(newUser.Username, newUser.RoleId);
                HttpContext.Session.SetString("JWT", token);

                _auditLogService.LogAction(AuditActions.USER_REGISTERED, newUser.Username, "Auth", newUser.UserId, $"{{\"action\": \"User Registered\", \"email\": \"{newUser.Email}\", \"roleId\": \"{newUser.RoleId}\"}}", "Info");

                TempData["Success"] = "Registration Successful! Please upload your KYC documents.";
                
                // Super Admin goes straight to Dashboard
                if (newUser.RoleId == 3)
                {
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                }

                // Everyone else goes straight to KYC Upload
                return RedirectToAction("Upload", "Document");
            }

            return RedirectToAction("Login");
        }

        // ────────────── REGISTER SUCCESS ──────────────
        public IActionResult RegisterSuccess()
        {
            if (TempData["Success"] == null)
                return RedirectToAction("Login");

            return View();
        }

        // ────────────── LOGIN (GET) ──────────────
        public IActionResult Login()
        {
            GenerateMathCaptcha();
            return View(new LoginModel());
        }

        // ────────────── LOGIN (POST) ──────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginModel model)
        {
            // Validate math captcha
            if (!ValidateMathCaptcha())
            {
                GenerateMathCaptcha();
                ViewBag.Error = "Incorrect captcha answer! Please try again ❌";
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                GenerateMathCaptcha();
                return View(model);
            }

            // Get user from Dapper
            var user = _userService.GetUserByUsername(model.Username);

            if (user == null)
            {
                GenerateMathCaptcha();
                ViewBag.Error = "Invalid Username ❌";
                return View(model);
            }

            // Block deactivated / soft-deleted users
            if (!user.IsActive || user.IsDeleted)
            {
                GenerateMathCaptcha();
                ViewBag.Error = "Your account has been deactivated. Contact the administrator.";
                return View(model);
            }

            // Verify password
            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(user, user.Password, model.Password);

            if (result != PasswordVerificationResult.Success)
            {
                GenerateMathCaptcha();
                ViewBag.Error = "Invalid Password ❌";
                return View(model);
            }

            // Session
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetInt32("RoleId", user.RoleId);
            HttpContext.Session.SetInt32("UserId", user.UserId);

            // JWT
            var token = JwtHelper.GenerateToken(user.Username, user.RoleId);
            HttpContext.Session.SetString("JWT", token);

            _auditLogService.LogAction(AuditActions.USER_LOGIN, user.Username, "Auth", user.UserId, $"{{\"action\": \"User Logged In\", \"email\": \"{user.Email}\"}}", "Info");

            TempData["Success"] = "Login Successful ✅";
            TempData["LoggedInUser"] = user.Fullname;

            // Super Admin always goes straight to dashboard
            if (user.RoleId == 3)
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            // Document check (For everyone except Super Admin)
            if (user.RoleId != 3 && user.DocStatus != "approved")
            {
                if (user.UploadAttempts >= 3)
                {
                    GenerateMathCaptcha();
                    ViewBag.Error = "Your account is permanently locked due to 3 failed KYC attempts. Please contact support.";
                    HttpContext.Session.Clear();
                    return View(model);
                }

                var userDocs = _docService.GetDocumentsByUserId(user.UserId);
                var hasDoc = userDocs.Any();
                var hasRejectedDoc = userDocs.Any(d => d.Status == "Rejected");

                // If no documents exist, OR master status is rejected, OR any individual doc is rejected -> go to Upload
                if (!hasDoc || user.DocStatus == "rejected" || hasRejectedDoc)
                    return RedirectToAction("Upload", "Document");
                    
                return RedirectToAction("Pending", "Document");
            }

            // If DocStatus is approved, they can access their respective dashboard
            if (user.RoleId == 1)
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }
            
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        // ────────────── LOGIN SUCCESS ──────────────
        public IActionResult LoginSuccess()
        {
            if (HttpContext.Session.GetString("Username") == null)
                return RedirectToAction("Login");

            return View();
        }

        // ────────────── LOGOUT ──────────────
        public IActionResult Logout()
        {
            var username = HttpContext.Session.GetString("Username");
            var userId = HttpContext.Session.GetInt32("UserId");
            if (username != null && userId != null)
            {
                _auditLogService.LogAction(AuditActions.USER_LOGOUT, username, "Auth", userId, "{\"action\": \"User Logged Out\"}", "Info");
            }

            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}