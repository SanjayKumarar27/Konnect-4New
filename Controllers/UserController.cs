using Konnect_4New.Models;
using Konnect_4New.Models.Dtos;
using Microsoft.AspNetCore.Http;
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
    public class UsersController : ControllerBase
    {
        private readonly Konnect4Context _context;

        public UsersController(Konnect4Context context)
        {
            _context = context;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserListDto>>> GetUsers()
        {
            return await _context.Users
                .Select(u => new UserListDto
                {
                    UserId = u.UserId, // Add UserId
                    Username = u.Username,
                    FullName = u.FullName,
                    ProfileImageUrl = u.ProfileImageUrl
                })
                .ToListAsync();
        }

        // GET: api/Users/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        // PUT: api/Users/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdateProfileDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Update fields if provided
            if (!string.IsNullOrWhiteSpace(dto.FullName))
                user.FullName = dto.FullName;

            if (!string.IsNullOrWhiteSpace(dto.Bio))
                user.Bio = dto.Bio;

            if (dto.IsPrivate.HasValue)
                user.IsPrivate = dto.IsPrivate.Value;

            if (!string.IsNullOrWhiteSpace(dto.ProfileImageUrl))
            {
                user.ProfileImageUrl = dto.ProfileImageUrl; // Store the provided URL directly
            }

            user.UpdatedAt = DateTime.UtcNow;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Profile updated successfully.",
                user = new
                {
                    user.UserId,
                    user.Username,
                    user.FullName,
                    user.Bio,
                    user.IsPrivate,
                    user.ProfileImageUrl
                }
            });
        }

        // GET: api/users/{id}/profile
        [HttpGet("{id}/profile")]
        public async Task<IActionResult> GetUserProfile(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User not found.");

            // Count stats
            var followersCount = await _context.Followers
                .CountAsync(f => f.UserId == id && f.Status == "Accepted");
            var followingCount = await _context.Followers
                .CountAsync(f => f.FollowerUserId == id && f.Status == "Accepted");
            var posts = await _context.Posts
                .Where(p => p.UserId == id)
                .Select(p => new PostDto
                {
                    PostId = p.PostId,
                    Content = p.Content,
                    ImageUrl = p.PostImageUrl,
                    CreatedAt = (DateTime)p.CreatedAt
                })
                .ToListAsync();

            var profile = new UserProfileDto
            {
                UserId = user.UserId,
                Username = user.Username,
                FullName = user.FullName,
                Bio = user.Bio,
                ProfileImageUrl = user.ProfileImageUrl,
                FollowersCount = followersCount,
                FollowingCount = followingCount,
                PostsCount = posts.Count,
                Posts = posts
            };

            return Ok(profile);
        }
    }
}