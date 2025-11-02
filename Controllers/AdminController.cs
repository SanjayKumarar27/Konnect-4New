using Konnect_4New.Models;
using Konnect_4New.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Konnect_4New.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly Konnect4Context _context;

        public AdminController(Konnect4Context context)
        {
            _context = context;
        }

        // ✅ Middleware: Check if user is admin
        private async Task<bool> IsAdminAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user?.Role == "Admin";
        }

        // GET: api/admin/dashboard
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard([FromQuery] int userId)
        {
            if (!await IsAdminAsync(userId))
                return Unauthorized("Only admins can access this resource.");

            try
            {
                var totalUsers = await _context.Users.CountAsync();
                var totalPosts = await _context.Posts.CountAsync();
                var totalComments = await _context.Comments.CountAsync();
                var activeUsersToday = await _context.Users
                    .CountAsync(u => u.UpdatedAt.Date == DateTime.UtcNow.Date);

                var recentUsers = await _context.Users
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(10)
                    .Include(u => u.Posts)
                    .Include(u => u.FollowerUsers)
                    .Select(u => new UserStatsDto
                    {
                        UserId = u.UserId,
                        Username = u.Username,
                        FullName = u.FullName ?? u.Username,
                        ProfileImageUrl = u.ProfileImageUrl,
                        PostsCount = u.Posts.Count,
                        FollowersCount = u.FollowerUsers.Count(f => f.Status == "Accepted"),
                        CreatedAt = u.CreatedAt,
                        Role = u.Role
                    })
                    .ToListAsync();

                var recentPosts = await _context.Posts
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(10)
                    .Include(p => p.User)
                    .Include(p => p.Likes)
                    .Include(p => p.Comments)
                    .Select(p => new PostStatsDto
                    {
                        PostId = p.PostId,
                        Content = p.Content ?? string.Empty,
                        Username = p.User.Username,
                        UserId = p.UserId,
                        LikesCount = p.Likes.Count,
                        CommentsCount = p.Comments.Count,
                        CreatedAt = p.CreatedAt
                    })
                    .ToListAsync();

                var dashboard = new AdminDashboardDto
                {
                    TotalUsers = totalUsers,
                    TotalPosts = totalPosts,
                    TotalComments = totalComments,
                    ActiveUsersToday = activeUsersToday,
                    RecentUsers = recentUsers,
                    RecentPosts = recentPosts
                };

                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error: {ex.Message}" });
            }
        }

        // GET: api/admin/users
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers([FromQuery] int userId)
        {
            if (!await IsAdminAsync(userId))
                return Unauthorized("Only admins can access this resource.");

            try
            {
                var users = await _context.Users
                    .Include(u => u.Posts)
                    .Include(u => u.FollowerUsers)
                    .Select(u => new UserStatsDto
                    {
                        UserId = u.UserId,
                        Username = u.Username,
                        FullName = u.FullName ?? u.Username,
                        ProfileImageUrl = u.ProfileImageUrl,
                        PostsCount = u.Posts.Count,
                        FollowersCount = u.FollowerUsers.Count(f => f.Status == "Accepted"),
                        CreatedAt = u.CreatedAt,
                        Role = u.Role
                    })
                    .OrderByDescending(u => u.CreatedAt)
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error: {ex.Message}" });
            }
        }

        // DELETE: api/admin/users/{userId}
        [HttpDelete("users/{targetUserId}")]
        public async Task<IActionResult> DeleteUser(int targetUserId, [FromQuery] int adminUserId)
        {
            if (!await IsAdminAsync(adminUserId))
                return Unauthorized("Only admins can delete users.");

            try
            {
                // Prevent deleting another admin
                var targetUser = await _context.Users.FindAsync(targetUserId);
                if (targetUser?.Role == "Admin")
                    return BadRequest("Cannot delete another admin.");

                if (targetUser == null)
                    return NotFound("User not found.");

                // Delete user (cascades to posts, comments, likes)
                _context.Users.Remove(targetUser);
                await _context.SaveChangesAsync();

                return Ok(new { message = "User deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error: {ex.Message}" });
            }
        }

        // GET: api/admin/posts
        [HttpGet("posts")]
        public async Task<IActionResult> GetAllPosts([FromQuery] int userId)
        {
            if (!await IsAdminAsync(userId))
                return Unauthorized("Only admins can access this resource.");

            try
            {
                var posts = await _context.Posts
                    .Include(p => p.User)
                    .Include(p => p.Likes)
                    .Include(p => p.Comments)
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new PostStatsDto
                    {
                        PostId = p.PostId,
                        Content = p.Content ?? string.Empty,
                        Username = p.User.Username,
                        ImageUrl = p.PostImageUrl,
                        UserId = p.UserId,
                        LikesCount = p.Likes.Count,
                        CommentsCount = p.Comments.Count,
                        CreatedAt = p.CreatedAt
                    })
                    .ToListAsync();

                return Ok(posts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error: {ex.Message}" });
            }
        }

        // DELETE: api/admin/posts/{postId}
        [HttpDelete("posts/{postId}")]
        public async Task<IActionResult> DeletePost(int postId, [FromQuery] int adminUserId)
        {
            if (!await IsAdminAsync(adminUserId))
                return Unauthorized("Only admins can delete posts.");

            try
            {
                var post = await _context.Posts
                    .Include(p => p.Comments)
                    .Include(p => p.Likes)
                    .FirstOrDefaultAsync(p => p.PostId == postId);

                if (post == null)
                    return NotFound("Post not found.");

                // Remove related data
                _context.Comments.RemoveRange(post.Comments);
                _context.Likes.RemoveRange(post.Likes);
                _context.Posts.Remove(post);

                await _context.SaveChangesAsync();

                return Ok(new { message = "Post deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error: {ex.Message}" });
            }
        }
    }
}