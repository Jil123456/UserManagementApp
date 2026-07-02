using Microsoft.AspNetCore.Mvc;
using UserManagementApp.Models;
using UserManagementApp.Services;

namespace UserManagementApp.Controllers
{
    public class AppealController : Controller
    {
        private readonly IUserService _userService;
        private readonly IAppealService _appealService;

        public AppealController(IUserService userService, IAppealService appealService)
        {
            _userService = userService;
            _appealService = appealService;
        }

        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("RejectedUserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = _userService.GetUserById(userId.Value);
            if (user == null || user.Status != "Rejected")
            {
                HttpContext.Session.Remove("RejectedUserId");
                return RedirectToAction("Login", "Auth");
            }

            var appeals = _appealService.GetAppealsByUserId(userId.Value);
            ViewBag.Appeals = appeals;
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SendMessage(string message)
        {
            var userId = HttpContext.Session.GetInt32("RejectedUserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!string.IsNullOrWhiteSpace(message))
            {
                var appeal = new UserAppeal
                {
                    UserId = userId.Value,
                    Message = message,
                    SentBy = "User",
                    SentDate = DateTime.UtcNow
                };
                _appealService.AddAppeal(appeal);

                // Change status to Pending so they appear in Admin's Pending Approvals queue
                _userService.UpdateUserStatus(userId.Value, "Pending");

                // Clear the RejectedUserId session since they are no longer purely Rejected
                HttpContext.Session.Remove("RejectedUserId");
                TempData["SuccessMessage"] = "Your request has been submitted to the admin for review.";
                return RedirectToAction("Login", "Auth");
            }

            return RedirectToAction("Index");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateDetails(string fullname, string email, string mobile, string password)
        {
            var userId = HttpContext.Session.GetInt32("RejectedUserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var user = _userService.GetUserById(userId.Value);
            if (user != null)
            {
                user.Fullname = fullname;
                user.Email = email;
                user.Mobile = mobile;
                _userService.UpdateUser(user);

                if (!string.IsNullOrWhiteSpace(password))
                {
                    var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();
                    var hashed = hasher.HashPassword(user, password);
                    _userService.UpdatePassword(user.UserId, hashed);
                }

                TempData["Success"] = "Your details have been updated! You can now send an appeal message to the admin.";
            }

            return RedirectToAction("Index");
        }
    }
}
