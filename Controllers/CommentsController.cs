
using Konnect_4New.Models;
using Konnect_4New.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Konnect_4.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    
    public class CommentsController : ControllerBase
    {
        private readonly Konnect4Context _context;

        public CommentsController(Konnect4Context context)
        {
            _context = context;
        }
        // GET COMMENTS BY POST ID
        [HttpGet("post/{postId}")]
        public async Task<IActionResult> GetCommentsByPost(int postId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return NotFound("Post not found.");

            var comments = await _context.Comments
                .Where(c => c.PostId == postId)
                .Include(c => c.User) // include user to get username
                .OrderBy(c => c.CreatedAt)
                .Select(c => new CommentDto
                {
                    CommentId = c.CommentId,
                    PostId = c.PostId,
                    ParentCommentId = c.ParentCommentId,
                    Content = c.Content,
                    Username = c.User.Username,
                    UserId = c.User.UserId,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            return Ok(comments);
        }

        // CREATE COMMENT
        [HttpPost]
        public async Task<IActionResult> CreateComment([FromBody] CreateCommentDto dto)
        {
            var post = await _context.Posts.FindAsync(dto.PostId);
            if (post == null) return NotFound("Post not found.");

            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null) return NotFound("User not found.");

            // If parent comment exists, check if it belongs to same post
            if (dto.ParentCommentId.HasValue)
            {
                var parent = await _context.Comments.FindAsync(dto.ParentCommentId.Value);
                if (parent == null || parent.PostId != dto.PostId)
                {
                    return BadRequest("Invalid parent comment.");
                }
            }

            var comment = new Comment
            {
                PostId = dto.PostId,
                UserId = dto.UserId,
                ParentCommentId = dto.ParentCommentId,
                Content = dto.Content,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Comment added successfully.", commentId = comment.CommentId });
        }

        // DELETE COMMENT (by comment owner OR post owner)
        [HttpDelete("{commentId}")]
        public async Task<IActionResult> DeleteComment(int commentId, [FromQuery] int userId)
        {
            var comment = await _context.Comments
                .Include(c => c.Post)
                .FirstOrDefaultAsync(c => c.CommentId == commentId);

            if (comment == null) return NotFound("Comment not found.");

            // Allow deletion if user is comment owner OR post owner
            if (comment.UserId != userId && comment.Post.UserId != userId)
            {
                return Unauthorized("You are not allowed to delete this comment.");
            }

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Comment deleted successfully." });
        }
    }
}
