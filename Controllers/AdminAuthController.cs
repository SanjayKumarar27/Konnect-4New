using Konnect_4New.Models;
using Konnect_4New.Models.Dtos;
using Konnect_4New.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Konnect_4New.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminAuthController : ControllerBase
    {
        private readonly Konnect4Context _context;
        private readonly IConfiguration _configuration;

        public AdminAuthController(Konnect4Context context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // 🔹 Admin login — only login, no register
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            // Check only in Admins table
            var admin = await _context.Admins
                .FirstOrDefaultAsync(a => a.Username == dto.Email || a.Email == dto.Email);

            if (admin == null || !AuthService.VerifyPassword(dto.Password, admin.PasswordHash))
            {
                return Unauthorized("Invalid admin credentials.");
            }

            // ✅ Generate JWT token
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
            var tokenHandler = new JwtSecurityTokenHandler();

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, admin.AdminId.ToString()),
                new Claim(ClaimTypes.Name, admin.Username),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(6),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new
            {
                message = "Admin login successful!",
                token = tokenString,
                admin = new { admin.AdminId, admin.Username, admin.Email }
            });
        }
    }
}
