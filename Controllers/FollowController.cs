using Konnect_4New.Models.Dtos;
using Konnect_4New.Models;
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

        // ✅ NEW: GET Followers List - api/follow/followers/{userId}
        [HttpGet("followers/{userId}")]
        public async Task<ActionResult<IEnumerable<FollowUserDto>>> GetFollowers(int userId)
        {
            var followers = await _context.Followers
                .Where(f => f.UserId == userId && f.Status == "Accepted")
                .Join(_context.Users,
                    f => f.FollowerUserId,
                    u => u.UserId,
                    (f, u) => new FollowUserDto
                    {
                        UserId = u.UserId,
                        Username = u.Username,
                        FullName = u.FullName,
                        ProfileImageUrl = u.ProfileImageUrl
                    })
                .ToListAsync();

            return Ok(followers);
        }

        // ✅ NEW: GET Following List - api/follow/following/{userId}
        [HttpGet("following/{userId}")]
        public async Task<ActionResult<IEnumerable<FollowUserDto>>> GetFollowing(int userId)
        {
            var following = await _context.Followers
                .Where(f => f.FollowerUserId == userId && f.Status == "Accepted")
                .Join(_context.Users,
                    f => f.UserId,
                    u => u.UserId,
                    (f, u) => new FollowUserDto
                    {
                        UserId = u.UserId,
                        Username = u.Username,
                        FullName = u.FullName,
                        ProfileImageUrl = u.ProfileImageUrl
                    })
                .ToListAsync();

            return Ok(following);
        }

        // POST: Follow
        [HttpPost("follow/{followerUserId}")]
        public async Task<IActionResult> FollowUser(int followerUserId, [FromBody] FollowDto dto)
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
                Status = targetUser.IsPrivate ? "Pending" : "Accepted" // Fixed: "ending" → "Pending"
            };

            _context.Followers.Add(follower);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = targetUser.IsPrivate ? "Follow request sent." : "You are now following this user.",
                status = follower.Status
            });
        }

        // DELETE: Unfollow
        [HttpDelete("unfollow/{followerUserId}")]
        public async Task<IActionResult> UnfollowUser(int followerUserId, [FromBody] FollowDto dto)
        {
            var follow = await _context.Followers
                .FirstOrDefaultAsync(f => f.UserId == dto.TargetUserId && f.FollowerUserId == followerUserId);

            if (follow == null) return NotFound("Follow relationship not found.");

            _context.Followers.Remove(follow);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Unfollowed successfully." });
        }

        // PUT: Accept Request
        [HttpPut("accept/{userId}")]
        public async Task<IActionResult> AcceptFollowRequest(int userId, [FromBody] FollowActionDto dto)
        {
            var request = await _context.Followers
                .FirstOrDefaultAsync(f => f.FollowerId == dto.RequestId && f.UserId == userId);

            if (request == null) return NotFound("Follow request not found.");

            if (request.Status == "Accepted") return BadRequest("Already accepted.");

            request.Status = "Accepted";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Follow request accepted." });
        }
    }
}