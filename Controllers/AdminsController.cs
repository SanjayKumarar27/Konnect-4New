using Konnect_4New.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;


namespace Konnect_4New.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly Konnect4Context _context;

        public AdminController(Konnect4Context context)
        {
            _context = context;
        }

        // 🔹 Get all users with their post count
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsersWithPostCount()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.UserId,
                    u.Username,
                    u.Email,
                    PostCount = _context.Posts.Count(p => p.UserId == u.UserId)
                })
                .ToListAsync();

            return Ok(users);
        }

        // 🔹 Get posts by user
        [HttpGet("users/{userId}/posts")]
        public async Task<IActionResult> GetUserPosts(int userId)
        {
            var posts = await _context.Posts
                .Where(p => p.UserId == userId)
                .ToListAsync();

            return Ok(posts);
        }

        // 🔹 Delete a specific post
        [HttpDelete("posts/{postId}")]
        public async Task<IActionResult> DeletePost(int postId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return NotFound();

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Post deleted successfully" });
        }

        // 🔹 Delete a specific user
        [HttpDelete("users/{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            // Delete user's posts first
            var posts = _context.Posts.Where(p => p.UserId == userId);
            _context.Posts.RemoveRange(posts);

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User and related posts deleted successfully" });
        }
    }
}
