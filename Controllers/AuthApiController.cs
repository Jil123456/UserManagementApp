using Microsoft.AspNetCore.Mvc;
using UserManagementApp.Models;
using UserManagementApp.Helpers;
using Microsoft.AspNetCore.Identity;
using System.Linq;

namespace UserManagementApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthApiController(AppDbContext context)
        {
            _context = context;
        }

        // REGISTER API
        [HttpPost("register")]
        public IActionResult Register([FromBody] User user)
        {
            if (_context.Users.Any(u => u.Username == user.Username))
                return BadRequest("Username already exists");

            var hasher = new PasswordHasher<User>();
            user.Password = hasher.HashPassword(user, user.Password);

            user.Dob = DateTime.SpecifyKind(user.Dob, DateTimeKind.Utc);
            user.RoleId = user.RoleId == 1 ? 1 : 2;
            user.CreatedDate = DateTime.UtcNow;

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok("User Registered Successfully ✅");
        }

        // LOGIN API
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginModel model)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == model.Username);

            if (user == null)
                return Unauthorized("Invalid Username");

            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(user, user.Password, model.Password);

            if (result != PasswordVerificationResult.Success)
                return Unauthorized("Invalid Password");

            // ✅ Generate JWT
            var token = JwtHelper.GenerateToken(user.Username, user.RoleId);

            return Ok(new
            {
                message = "Login Successful ✅",
                token = token
            });
        }
    }
}