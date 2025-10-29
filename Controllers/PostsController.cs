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
    public class PostsController : ControllerBase
    {
        private readonly Konnect4Context _context;
        public PostsController(Konnect4Context context)
        {
            _context = context;
        }

        // CREATE POST
        [HttpPost]
        public async Task<IActionResult> CreatePost([FromBody] CreatePostDto dto)
        {
            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null) return NotFound("User not found.");

            var post = new Post
            {
                UserId = dto.UserId,
                Content = dto.Content,
                PostImageUrl = dto.ImageUrl,
                Category = dto.Category?.Trim().ToLower(),  // ← ADDED: Save in lowercase
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Post created successfully.", postId = post.PostId });
        }

        // UPDATE POST (only if user owns it)
        [HttpPut("{postId}")]
        public async Task<IActionResult> UpdatePost(int postId, [FromBody] UpdatePostDto dto, [FromQuery] int userId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return NotFound("Post not found.");
            if (post.UserId != userId) return Unauthorized("You can only update your own posts.");
            if (!string.IsNullOrWhiteSpace(dto.Content))
                post.Content = dto.Content;
            if (dto.ImageUrl != null)
                post.PostImageUrl = dto.ImageUrl;
            post.UpdatedAt = DateTime.UtcNow;
            _context.Posts.Update(post);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Post updated successfully." });
        }

        // GET USER'S POSTS
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserPosts(int userId)
        {
            var posts = await _context.Posts
                .Where(p => p.UserId == userId)
                .Include(p => p.User)
                .Include(p => p.Comments)
                .Include(p => p.Likes)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PostResponseDto
                {
                    PostId = p.PostId,
                    Content = p.Content,
                    ImageUrl = p.PostImageUrl,
                    CreatedAt = p.CreatedAt,
                    Username = p.User.Username,
                    LikesCount = p.Likes.Count,
                    CommentsCount = p.Comments.Count
                })
                .ToListAsync();
            return Ok(posts);
        }

        // GET POSTS FROM FOLLOWING USERS
        [HttpGet("feed/{userId}")]
        public async Task<IActionResult> GetFeed(int userId)
        {
            var followingIds = await _context.Followers
                .Where(f => f.FollowerUserId == userId && f.Status == "Accepted")
                .Select(f => f.UserId)
                .ToListAsync();
            var posts = await _context.Posts
                .Where(p => followingIds.Contains(p.UserId) || p.UserId == userId)
                .Include(p => p.User)
                .Include(p => p.Comments)
                .Include(p => p.Likes)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PostResponseDto
                {
                    PostUserId = p.UserId,
                    PostId = p.PostId,
                    Content = p.Content,
                    ImageUrl = p.PostImageUrl,
                    CreatedAt = p.CreatedAt,
                    Username = p.User.Username,
                    LikesCount = p.Likes.Count,
                    CommentsCount = p.Comments.Count
                })
                .ToListAsync();
            return Ok(posts);
        }

        // DELETE POST (only if user owns it)
        [HttpDelete("{postId}")]
        public async Task<IActionResult> DeletePost(int postId, [FromQuery] int userId)
        {
            var post = await _context.Posts
                .Include(p => p.Comments)
                .Include(p => p.Likes)
                .FirstOrDefaultAsync(p => p.PostId == postId);
            if (post == null)
                return NotFound("Post not found.");
            if (post.UserId != userId)
                return Unauthorized("You can only delete your own posts.");
            _context.Comments.RemoveRange(post.Comments);
            _context.Likes.RemoveRange(post.Likes);
            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Post deleted successfully." });
        }

        // ————————————————————————
        // UPDATED: Explore with case-insensitive category filter
        // ————————————————————————
        [HttpGet("explore")]
        public async Task<IActionResult> GetExplorePosts([FromQuery] string? category = null)
        {
            try
            {
                var query = _context.Posts
                    .Include(p => p.User)
                    .Include(p => p.Likes)
                    .Include(p => p.Comments)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(category))
                {
                    var searchCat = category.Trim().ToLower();  // ← Normalize input
                    query = query.Where(p => p.Category != null &&
                                            p.Category.ToLower() == searchCat);  // ← Case-insensitive match
                }

                var randomPosts = await query
                    .OrderBy(p => Guid.NewGuid())
                    .Take(20)
                    .Select(p => new
                    {
                        p.PostId,
                        p.UserId,
                        Username = p.User.Username,
                        p.Content,
                        ImageUrl = p.PostImageUrl,
                        p.CreatedAt,
                        LikesCount = p.Likes.Count,
                        CommentsCount = p.Comments.Count,
                        Liked = false,
                        ProfileImageUrl = p.User.ProfileImageUrl,
                        p.Category
                    })
                    .ToListAsync();

                return Ok(randomPosts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load explore posts", error = ex.Message });
            }
        }
    }
}