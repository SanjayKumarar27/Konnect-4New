// File: Konnect_4New.Tests/Controllers/LikesControllerTests.cs
using Konnect_4.Controllers;
using Konnect_4New.Controllers;
using Konnect_4New.Models;
using Konnect_4New.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Threading.Tasks;


namespace Konnect_4New.Tests.Controllers
{
    [TestFixture]
    public class LikesControllerTests
    {
        private Konnect4Context _context = null!;
        private LikesController _controller = null!;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<Konnect4Context>()
                .UseInMemoryDatabase($"LikesDb_{Guid.NewGuid()}")
                .Options;

            _context = new Konnect4Context(options);
            _controller = new LikesController(_context);

            SeedData();
        }

        private void SeedData()
        {
            var user = new User { UserId = 1, Username = "alice", Email = "a@a.com", PasswordHash = "x" };
            var post = new Post { PostId = 1, UserId = 1, Content = "Hello", CreatedAt = DateTime.UtcNow };

            _context.Users.Add(user);
            _context.Posts.Add(post);
            _context.SaveChanges();
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        // TEST 1: Like post – success
        [Test]
        public async Task LikePost_ValidData_ReturnsSuccessAndLikeId()
        {
            var dto = new LikeDto { PostId = 1, UserId = 1 };

            var result = await _controller.LikePost(dto);
            var ok = result as OkObjectResult;

            Assert.That(ok, Is.Not.Null);
            Assert.That(ok.Value, Is.Not.Null);

            var message = ok.Value.GetType().GetProperty("message")?.GetValue(ok.Value);
            var likeId = ok.Value.GetType().GetProperty("likeId")?.GetValue(ok.Value);

            Assert.That(message, Is.EqualTo("Post liked successfully."));
            Assert.That(likeId, Is.Not.Null);

            var likeInDb = await _context.Likes.FirstOrDefaultAsync();
            Assert.That(likeInDb, Is.Not.Null);
        }

        // TEST 2: Like post – already liked
        [Test]
        public async Task LikePost_AlreadyLiked_ReturnsBadRequest()
        {
            // First like
            await _controller.LikePost(new LikeDto { PostId = 1, UserId = 1 });

            // Try again
            var result = await _controller.LikePost(new LikeDto { PostId = 1, UserId = 1 });
            var bad = result as BadRequestObjectResult;

            Assert.That(bad?.Value, Is.EqualTo("You already liked this post."));
        }

        // TEST 3: Unlike post – success
        [Test]
        public async Task UnlikePost_ValidLike_RemovesLike()
        {
            // First, like it
            await _controller.LikePost(new LikeDto { PostId = 1, UserId = 1 });
            var like = await _context.Likes.FirstAsync();

            var result = await _controller.UnlikePost(1, 1);
            var ok = result as OkObjectResult;

            var message = ok?.Value?.GetType().GetProperty("message")?.GetValue(ok.Value);
            Assert.That(message, Is.EqualTo("Post unliked successfully."));

            var removed = await _context.Likes.FindAsync(like.LikeId);
            Assert.That(removed, Is.Null);
        }

        // TEST 4: Get likes for post – returns list with usernames
        [Test]
        public async Task GetPostLikes_ExistingLikes_ReturnsListWithUsernames()
        {
            // Add another user
            var bob = new User { UserId = 2, Username = "bob", Email = "b@b.com", PasswordHash = "y" };
            _context.Users.Add(bob);
            await _context.SaveChangesAsync();

            // Both like the post
            await _controller.LikePost(new LikeDto { PostId = 1, UserId = 1 });
            await _controller.LikePost(new LikeDto { PostId = 1, UserId = 2 });

            var result = await _controller.GetPostLikes(1);
            var ok = result as OkObjectResult;
            var likes = ok?.Value as List<LikeResponseDto>;

            Assert.That(likes, Has.Count.EqualTo(2));
            Assert.That(likes[0].Username, Is.EqualTo("alice").Or.EqualTo("bob"));
            Assert.That(likes[1].Username, Is.EqualTo("alice").Or.EqualTo("bob"));
        }
    }
}