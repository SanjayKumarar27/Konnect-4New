using Konnect_4.Controllers;
using Konnect_4New.Controllers;
using Konnect_4New.Models;
using Konnect_4New.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Konnect_4New.Tests.Controllers
{
    [TestFixture]
    public class FollowControllerTests
    {
        private Konnect4Context _context = null!;
        private FollowController _controller = null!;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<Konnect4Context>()
                .UseInMemoryDatabase(databaseName: $"FollowTestDb_{Guid.NewGuid()}")
                .Options;

            _context = new Konnect4Context(options);
            _controller = new FollowController(_context);

            SeedData();
        }

        // ================================================================
        // SEED DATA – All users, follows, private/public accounts
        // ================================================================
        private void SeedData()
        {
            // Users
            var alice = new User { UserId = 1, Username = "alice", Email = "a@a.com", PasswordHash = "x", FullName = "Alice Smith", ProfileImageUrl = "alice.jpg", IsPrivate = false };
            var bob = new User { UserId = 2, Username = "bob", Email = "b@b.com", PasswordHash = "y", FullName = "Bob Johnson", ProfileImageUrl = "bob.jpg", IsPrivate = true };
            var charlie = new User { UserId = 3, Username = "charlie", Email = "c@c.com", PasswordHash = "z", FullName = "Charlie Brown", ProfileImageUrl = "charlie.jpg", IsPrivate = false };
            var diana = new User { UserId = 4, Username = "diana", Email = "d@d.com", PasswordHash = "w", FullName = "Diana Prince", ProfileImageUrl = "diana.jpg", IsPrivate = true };

            // Follow relationships
            var follow1 = new Follower { FollowerId = 101, UserId = 2, FollowerUserId = 1, Status = "Accepted" };   // alice → bob (accepted)
            var follow2 = new Follower { FollowerId = 102, UserId = 3, FollowerUserId = 1, Status = "Accepted" };   // alice → charlie (accepted)
            var follow3 = new Follower { FollowerId = 103, UserId = 4, FollowerUserId = 1, Status = "Pending" };   // alice → diana (pending)
            var follow4 = new Follower { FollowerId = 104, UserId = 1, FollowerUserId = 3, Status = "Accepted" };   // charlie → alice (accepted)
            var follow5 = new Follower { FollowerId = 105, UserId = 2, FollowerUserId = 3, Status = "Accepted" };   // charlie → bob (accepted)

            _context.Users.AddRange(alice, bob, charlie, diana);
            _context.Followers.AddRange(follow1, follow2, follow3, follow4, follow5);
            _context.SaveChanges();
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        // ================================================================
        // GET /api/follow/followers/{userId}
        // ================================================================

        [Test]
        public async Task GetFollowers_PublicUser_ReturnsAcceptedFollowers()
        {
            var result = await _controller.GetFollowers(1); // alice's followers
            var ok = result.Result as OkObjectResult;
            var followers = ok?.Value as List<FollowUserDto>;

            Assert.That(ok, Is.Not.Null);
            Assert.That(followers, Has.Count.EqualTo(1)); // only charlie follows alice
            Assert.That(followers[0].Username, Is.EqualTo("charlie"));
        }

        [Test]
        public async Task GetFollowers_PrivateUser_ReturnsOnlyAccepted()
        {
            var result = await _controller.GetFollowers(2); // bob's followers
            var ok = result.Result as OkObjectResult;
            var followers = ok?.Value as List<FollowUserDto>;

            Assert.That(followers, Has.Count.EqualTo(2)); // alice and charlie
            Assert.That(followers.Any(f => f.Username == "alice"), Is.True);
            Assert.That(followers.Any(f => f.Username == "charlie"), Is.True);
        }

        [Test]
        public async Task GetFollowers_NoFollowers_ReturnsEmptyList()
        {
            var result = await _controller.GetFollowers(4); // diana has no accepted followers
            var ok = result.Result as OkObjectResult;
            var followers = ok?.Value as List<FollowUserDto>;
            Assert.That(followers, Is.Empty);
        }

        // ================================================================
        // GET /api/follow/following/{userId}
        // ================================================================

        [Test]
        public async Task GetFollowing_ReturnsAcceptedFollowing()
        {
            var result = await _controller.GetFollowing(1); // alice follows
            var ok = result.Result as OkObjectResult;
            var following = ok?.Value as List<FollowUserDto>;

            Assert.That(following, Has.Count.EqualTo(2)); // bob and charlie
            Assert.That(following.Any(f => f.Username == "bob"), Is.True);
            Assert.That(following.Any(f => f.Username == "charlie"), Is.True);
        }

        [Test]
        public async Task GetFollowing_NoFollowing_ReturnsEmptyList()
        {
            var result = await _controller.GetFollowing(4); // diana follows no one
            var ok = result.Result as OkObjectResult;
            var following = ok?.Value as List<FollowUserDto>;
            Assert.That(following, Is.Empty);
        }

        // ================================================================
        // POST /api/follow/follow/{followerUserId}
        // ================================================================

        [Test]
        public async Task FollowUser_PublicAccount_AcceptsImmediately()
        {
            var dto = new FollowDto { TargetUserId = 3 }; // charlie (public)
            var result = await _controller.FollowUser(4, dto); // diana follows charlie

            var ok = result as OkObjectResult;
            var payload = ok?.Value;
            var message = payload?.GetType().GetProperty("message")?.GetValue(payload);
            var status = payload?.GetType().GetProperty("status")?.GetValue(payload);

            Assert.That(message, Is.EqualTo("You are now following this user."));
            Assert.That(status, Is.EqualTo("Accepted"));

            var follow = await _context.Followers
                .FirstOrDefaultAsync(f => f.UserId == 3 && f.FollowerUserId == 4);
            Assert.That(follow, Is.Not.Null);
            Assert.That(follow.Status, Is.EqualTo("Accepted"));
        }


        [Test]
        public async Task FollowUser_TargetNotFound_ReturnsNotFound()
        {
            var dto = new FollowDto { TargetUserId = 999 };
            var result = await _controller.FollowUser(1, dto);
            var notFound = result as NotFoundObjectResult;
            Assert.That(notFound?.Value, Is.EqualTo("Target user not found."));
        }

        [Test]
        public async Task FollowUser_AlreadyFollowing_ReturnsBadRequest()
        {
            var dto = new FollowDto { TargetUserId = 2 }; // alice already follows bob
            var result = await _controller.FollowUser(1, dto);
            var bad = result as BadRequestObjectResult;
            Assert.That(bad?.Value, Is.EqualTo("Already following or request pending."));
        }

        // ================================================================
        // DELETE /api/follow/unfollow/{followerUserId}
        // ================================================================

        [Test]
        public async Task UnfollowUser_Valid_UnfollowsSuccessfully()
        {
            var dto = new FollowDto { TargetUserId = 2 }; // alice unfollows bob
            var result = await _controller.UnfollowUser(1, dto);

            var ok = result as OkObjectResult;
            var message = ok?.Value?.GetType().GetProperty("message")?.GetValue(ok.Value);
            Assert.That(message, Is.EqualTo("Unfollowed successfully."));

            var follow = await _context.Followers
                .FirstOrDefaultAsync(f => f.UserId == 2 && f.FollowerUserId == 1);
            Assert.That(follow, Is.Null);
        }



        // ================================================================
        // PUT /api/follow/accept/{userId}
        // ================================================================

        [Test]
        public async Task AcceptFollowRequest_Valid_AcceptsRequest()
        {
            var dto = new FollowActionDto { RequestId = 103 }; // alice's pending request to diana
            var result = await _controller.AcceptFollowRequest(4, dto); // diana accepts

            var ok = result as OkObjectResult;
            var message = ok?.Value?.GetType().GetProperty("message")?.GetValue(ok.Value);
            Assert.That(message, Is.EqualTo("Follow request accepted."));

            var request = await _context.Followers.FindAsync(103);
            Assert.That(request?.Status, Is.EqualTo("Accepted"));
        }

        [Test]
        public async Task AcceptFollowRequest_NotFound_ReturnsNotFound()
        {
            var dto = new FollowActionDto { RequestId = 999 };
            var result = await _controller.AcceptFollowRequest(4, dto);
            var notFound = result as NotFoundObjectResult;
            Assert.That(notFound?.Value, Is.EqualTo("Follow request not found."));
        }

        [Test]
        public async Task AcceptFollowRequest_AlreadyAccepted_ReturnsBadRequest()
        {
            var dto = new FollowActionDto { RequestId = 101 }; // already accepted
            var result = await _controller.AcceptFollowRequest(2, dto);
            var bad = result as BadRequestObjectResult;
            Assert.That(bad?.Value, Is.EqualTo("Already accepted."));
        }
    }
}