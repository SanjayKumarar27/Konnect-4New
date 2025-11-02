using Konnect_4New.Models;
using Konnect_4New.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Konnect_4.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    
    public class LikesController : ControllerBase
    {
        private readonly Konnect4Context _context;

        public LikesController(Konnect4Context context)
        {
            _context = context;
        }

        // POST: api/likes (Like a post)
        [HttpPost]
        public async Task<IActionResult> LikePost([FromBody] LikeDto dto)
        {
            var post = await _context.Posts.FindAsync(dto.PostId);
            if (post == null) return NotFound("Post not found.");

            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null) return NotFound("User not found.");

            var existingLike = await _context.Likes
                .FirstOrDefaultAsync(l => l.PostId == dto.PostId && l.UserId == dto.UserId);

            if (existingLike != null) return BadRequest("You already liked this post.");

            var like = new Like
            {
                PostId = dto.PostId,
                UserId = dto.UserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Likes.Add(like);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Post liked successfully.", likeId = like.LikeId });
        }

        // DELETE: api/likes (Unlike a post)
        [HttpDelete]
        public async Task<IActionResult> UnlikePost([FromQuery] int postId, [FromQuery] int userId)
        {
            var like = await _context.Likes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

            if (like == null) return NotFound("Like not found.");

            _context.Likes.Remove(like);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Post unliked successfully." });
        }

        // GET: api/likes/{postId} (Get all likes for a post)
        [HttpGet("{postId}")]
        public async Task<IActionResult> GetPostLikes(int postId)
        {
            var likes = await _context.Likes
                .Where(l => l.PostId == postId)
                .Include(l => l.User)
                .Select(l => new LikeResponseDto
                {
                    LikeId = l.LikeId,
                    UserId = l.UserId,
                    Username = l.User.Username,
                    CreatedAt = l.CreatedAt
                })
                .ToListAsync();

            return Ok(likes);
        }
    }
}
