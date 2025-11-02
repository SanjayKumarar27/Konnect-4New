// Controllers/AuthController.cs - UPDATED
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
        private readonly IJwtService _jwtService; // ✅ NEW: Inject JWT Service

        public AuthController(Konnect4Context context, IJwtService jwtService) // ✅ NEW: Add JWT Service to constructor
        {
            _context = context;
            _jwtService = jwtService;
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
                PasswordHash = AuthService.HashPassword(dto.Password),
                Role = "User" // ✅ Default role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Registration successful!" });
        }

        // POST: api/auth/login - ✅ UPDATED TO RETURN JWT TOKEN
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email || u.Username == dto.Email);

            if (user == null || !AuthService.VerifyPassword(dto.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid credentials.");
            }

            // ✅ NEW: Generate JWT token
            var jwtToken = _jwtService.GenerateToken(user);

            return Ok(new
            {
                message = "Login successful!",
                token = jwtToken, // ✅ Return JWT token
                user = new
                {
                    user.UserId,
                    user.Username,
                    user.Email,
                    user.FullName,
                    user.Role // ✅ Include role
                }
            });
        }

        // ✅ NEW: Refresh token endpoint
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
        {
            if (string.IsNullOrEmpty(dto.Token))
                return BadRequest("Token is required.");

            // ✅ Validate old token
            var userId = _jwtService.GetUserIdFromToken(dto.Token);
            if (!userId.HasValue)
                return Unauthorized("Invalid token.");

            // ✅ Get user and generate new token
            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
                return NotFound("User not found.");

            var newToken = _jwtService.GenerateToken(user);

            return Ok(new
            {
                token = newToken,
                user = new
                {
                    user.UserId,
                    user.Username,
                    user.Email,
                    user.FullName,
                    user.Role
                }
            });
        }

        // ✅ NEW: Validate token endpoint (for frontend to check token validity)
        [HttpPost("validate-token")]
        public IActionResult ValidateToken([FromBody] ValidateTokenDto dto)
        {
            if (string.IsNullOrEmpty(dto.Token))
                return BadRequest("Token is required.");

            var principal = _jwtService.ValidateToken(dto.Token);
            if (principal == null)
                return Unauthorized("Invalid or expired token.");

            var userId = _jwtService.GetUserIdFromToken(dto.Token);
            var role = _jwtService.GetUserRoleFromToken(dto.Token);

            return Ok(new
            {
                isValid = true,
                userId = userId,
                role = role
            });
        }
    }
}
