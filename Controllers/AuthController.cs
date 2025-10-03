using Konnect_4New.Models;
using Konnect_4New.Models.Dtos;
using Konnect_4New.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Konnect_4New.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly Konnect4Context _context;

        public AuthController(Konnect4Context context)
        {
            _context = context;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email || u.Username == dto.Username))
            {
                return BadRequest("Email or Username already exists.");
            }

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                FullName = dto.FullName,
                PasswordHash = AuthService.HashPassword(dto.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Registration successful!" });
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email || u.Username == dto.Email);

            if (user == null || !AuthService.VerifyPassword(dto.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid credentials.");
            }

            return Ok(new
            {
                message = "Login successful!",
                user = new { user.UserId, user.Username, user.Email, user.FullName }
            });
        }
    }
}
