using Microsoft.AspNetCore.Mvc;
using UserManagementApp.Helpers;
using UserManagementApp.Services;
using UserManagementApp.Models;
using Microsoft.AspNetCore.Identity;

namespace UserManagementApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthApiController : ControllerBase
    {
        private readonly IUserService _userService;

        public AuthApiController(IUserService userService)
        {
            _userService = userService;
        }

        // REGISTER API
        [HttpPost("register")]
        public IActionResult Register([FromBody] User user)
        {
            // Duplicate username check
            var existing = _userService.GetUserByUsername(user.Username);
            if (existing != null)
                return BadRequest("Username already exists");

            var hasher = new PasswordHasher<User>();
            user.Password = hasher.HashPassword(user, user.Password);

            user.Dob = DateTime.SpecifyKind(user.Dob, DateTimeKind.Utc);
            user.RoleId = user.RoleId == 1 ? 1 : 2;
            user.CreatedDate = DateTime.UtcNow;

            _userService.AddUser(user);

            return Ok("User Registered Successfully ✅");
        }

        // LOGIN API
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginModel model)
        {
            var user = _userService.GetUserByUsername(model.Username);

            if (user == null)
                return Unauthorized("Invalid Username");

            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(user, user.Password, model.Password);

            if (result != PasswordVerificationResult.Success)
                return Unauthorized("Invalid Password");

            var token = JwtHelper.GenerateToken(user.Username, user.RoleId);

            return Ok(new
            {
                message = "Login Successful ✅",
                token = token
            });
        }
    }
}