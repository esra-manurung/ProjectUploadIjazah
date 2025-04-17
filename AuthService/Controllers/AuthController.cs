using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Shared.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private const string JwtSecretKey = "super_secret_key_that_is_very_secure_123";
        [HttpPost("login")]
        [AllowAnonymous] // <-- agar bisa diakses tanpa token
        public IActionResult Login([FromBody] UserDto request)
        {
            if (request.Username == "admin" && request.Password == "password")
            {
                var token = GenerateJwtToken(request.Username);
                return Ok(new { token });
            }

            return Unauthorized();
        }
        private string GenerateJwtToken(string username)
        {
            var key = Encoding.ASCII.GetBytes(JwtSecretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.Name, username)
        }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
