
using Konnect_4New.Models.Dtos;
using Konnect_4New.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Konnect_4.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FollowController : ControllerBase
    {
        private readonly Konnect4Context _context;

        public FollowController(Konnect4Context context)
        {
            _context = context;
        }

        // POST: api/follow (Follow or Request)
        [HttpPost("follow/{followerUserId}")]
        public async Task<IActionResult> FollowUser(int followerUserId, FollowDto dto)
        {
            var targetUser = await _context.Users.FindAsync(dto.TargetUserId);
            if (targetUser == null) return NotFound("Target user not found.");

            var existing = await _context.Followers
                .FirstOrDefaultAsync(f => f.UserId == dto.TargetUserId && f.FollowerUserId == followerUserId);

            if (existing != null) return BadRequest("Already following or request pending.");

            var follower = new Follower
            {
                UserId = dto.TargetUserId,
                FollowerUserId = followerUserId,
                Status= targetUser.IsPrivate==true ? "ending" : "Accepted"
            };

            _context.Followers.Add(follower);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = targetUser.IsPrivate==true ? "Follow request sent." : "You are now following this user.",
                status = follower.Status
            });
        }

        // DELETE: api/follow/unfollow
        [HttpDelete("unfollow/{followerUserId}")]
        public async Task<IActionResult> UnfollowUser(int followerUserId, FollowDto dto)
        {
            var follow = await _context.Followers
                .FirstOrDefaultAsync(f => f.UserId == dto.TargetUserId && f.FollowerUserId == followerUserId);

            if (follow == null) return NotFound("Follow relationship not found.");

            _context.Followers.Remove(follow);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Unfollowed successfully." });
        }

        // PUT: api/follow/accept
        [HttpPut("accept/{userId}")]
        public async Task<IActionResult> AcceptFollowRequest(int userId, FollowActionDto dto)
        {
            var request = await _context.Followers
                .FirstOrDefaultAsync(f => f.FollowerId == dto.RequestId && f.UserId == userId);

            if (request == null) return NotFound("Follow request not found.");

            if (request.Status == "Accepted") return BadRequest("Already accepted.");

            request.Status = "Accepted";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Follow request accepted." });
        }

        // POST: api/follow/followback
        [HttpPost("followback/{userId}")]
        public async Task<IActionResult> FollowBack(int userId, FollowDto dto)
        {
            var targetUser = await _context.Users.FindAsync(dto.TargetUserId);
            if (targetUser == null) return NotFound("Target user not found.");

            var existing = await _context.Followers
                .FirstOrDefaultAsync(f => f.UserId == dto.TargetUserId && f.FollowerUserId == userId);

            if (existing != null) return BadRequest("Already following or request pending.");

            var follower = new Follower
            {
                UserId = dto.TargetUserId,
                FollowerUserId = userId,
                Status = targetUser.IsPrivate == true ? "Pending" : "Accepted"
            };

            _context.Followers.Add(follower);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = targetUser.IsPrivate == true ? "Follow back request sent." : "You are now following back.",
                status = follower.Status
            });
        }
    
}
}
