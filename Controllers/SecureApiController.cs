using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace UserManagementApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SecureApiController : ControllerBase
    {
        // 🔐 Any logged-in user
        [Authorize]
        [HttpGet("data")]
        public IActionResult GetSecureData()
        {
            var username = User.Identity?.Name;
            var roleId = User.FindFirst("RoleId")?.Value;

            return Ok(new
            {
                message = "This is protected data 🔐",
                user = username,
                role = roleId
            });
        }

        // 🔐 Admin only (RoleId = 1)
        [Authorize]
        [HttpGet("admin")]
        public IActionResult GetAdminData()
        {
            var roleId = User.FindFirst("RoleId")?.Value;

            if (roleId != "1")
            {
                return Forbid(); // ❌ Not admin
            }

            return Ok("Admin only data 👑");
        }

        // 🔐 Get current user info
        [Authorize]
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            var username = User.Identity?.Name;
            var roleId = User.FindFirst("RoleId")?.Value;

            return Ok(new
            {
                username = username,
                role = roleId
            });
        }
    }
}