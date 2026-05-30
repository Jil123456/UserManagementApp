using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace UserManagementApp.Helpers
{
    public class JwtHelper
    {
        public static string GenerateToken(string username, int roleId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            // ✅ Correct key
            var key = Encoding.UTF8.GetBytes("THIS_IS_MY_SUPER_SECRET_KEY_12345678901234567890");

            // ✅ Correct credentials
            var creds = new SigningCredentials(
                new SymmetricSecurityKey(key), // ✔ byte[] used here
                SecurityAlgorithms.HmacSha256
            );

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim("RoleId", roleId.ToString())
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            return tokenHandler.WriteToken(token);
        }
    }
}
