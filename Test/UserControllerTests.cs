
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
    public class UsersControllerTests
    {
        private Konnect4Context _context = null!;
        private UsersController _controller = null!;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<Konnect4Context>()
                .UseInMemoryDatabase($"UsersDb_{Guid.NewGuid()}")
                .Options;

            _context = new Konnect4Context(options);
            _controller = new UsersController(_context);

            SeedData();
        }

        private void SeedData()
        {
            var users = new[]
            {
                new User
                {
                    UserId = 1,
                    Username = "alice",
                    Email = "a@a.com",
                    PasswordHash = "x",
                    FullName = "Alice Smith",
                    ProfileImageUrl = "alice.jpg",
                    Bio = "Hello!",
                    IsPrivate = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    UserId = 2,
                    Username = "bob",
                    Email = "b@b.com",
                    PasswordHash = "y",
                    FullName = "Bob Johnson",
                    ProfileImageUrl = "bob.jpg",
                    Bio = "Hi there",
                    IsPrivate = true
                }
            };

            var posts = new[]
            {
                new Post { PostId = 1, UserId = 1, Content = "Post 1", PostImageUrl = "img1.jpg", CreatedAt = DateTime.UtcNow },
                new Post { PostId = 2, UserId = 1, Content = "Post 2", PostImageUrl = "img2.jpg", CreatedAt = DateTime.UtcNow }
            };

            var follow = new Follower { FollowerId = 101, UserId = 1, FollowerUserId = 2, Status = "Accepted" };

            _context.Users.AddRange(users);
            _context.Posts.AddRange(posts);
            _context.Followers.Add(follow);
            _context.SaveChanges();
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        // TEST 1: GET /api/users
        [Test]
        public async Task GetUsers_ReturnsAllUsersWithUserId()
        {
            var result = await _controller.GetUsers();

            // FIXED: Use .Value directly (it's List<UserListDto>)
            var users = result.Value as List<UserListDto>;

            Assert.That(users, Is.Not.Null);
            Assert.That(users, Has.Count.EqualTo(2));
            Assert.That(users.Any(u => u.UserId == 1 && u.Username == "alice"), Is.True);
            Assert.That(users.Any(u => u.UserId == 2 && u.Username == "bob"), Is.True);
        }

        // TEST 2: GET /api/users/{id}
        [Test]
        public async Task GetUser_ValidId_ReturnsUser()
        {
            var result = await _controller.GetUser(1);

            // FIXED: Use .Value (it's User)
            var user = result.Value;

            Assert.That(user, Is.Not.Null);
            Assert.That(user.UserId, Is.EqualTo(1));
            Assert.That(user.Username, Is.EqualTo("alice"));
        }

        // TEST 3: PUT /api/users/{id}
        [Test]
        public async Task UpdateProfile_ValidData_UpdatesFieldsAndReturnsMessage()
        {
            var dto = new UpdateProfileDto
            {
                FullName = "Alice Updated",
                Bio = "New bio",
                IsPrivate = true,
                ProfileImageUrl = "new.jpg"
            };

            var result = await _controller.UpdateProfile(1, dto);
            var ok = result as OkObjectResult;

            Assert.That(ok, Is.Not.Null);
            Assert.That(ok.Value, Is.Not.Null);

            var message = ok.Value.GetType().GetProperty("message")?.GetValue(ok.Value);
            var userObj = ok.Value.GetType().GetProperty("user")?.GetValue(ok.Value);

            Assert.That(message, Is.EqualTo("Profile updated successfully."));

            var fullName = userObj?.GetType().GetProperty("FullName")?.GetValue(userObj);
            var bio = userObj?.GetType().GetProperty("Bio")?.GetValue(userObj);
            var isPrivate = userObj?.GetType().GetProperty("IsPrivate")?.GetValue(userObj);

            Assert.That(fullName, Is.EqualTo("Alice Updated"));
            Assert.That(bio, Is.EqualTo("New bio"));
            Assert.That(isPrivate, Is.EqualTo(true));
        }

        // TEST 4: GET /api/users/{id}/profile
        [Test]
        public async Task GetUserProfile_PublicUser_ReturnsProfileWithStatsAndPosts()
        {
            var result = await _controller.GetUserProfile(1, 999);
            var ok = result as OkObjectResult;
            var profile = ok?.Value as UserProfileDto;

            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.UserId, Is.EqualTo(1));
            Assert.That(profile.FullName, Is.EqualTo("Alice Smith"));
            Assert.That(profile.PostsCount, Is.EqualTo(2));
            Assert.That(profile.FollowersCount, Is.EqualTo(1));
            Assert.That(profile.FollowingCount, Is.EqualTo(0));
            Assert.That(profile.IsFollowing, Is.False);
            Assert.That(profile.Posts, Has.Count.EqualTo(2));
        }

        // TEST 5: GET /api/users/{id}/profile?currentUserId=2
        [Test]
        public async Task GetUserProfile_FollowingUser_SetsIsFollowingTrue()
        {
            var result = await _controller.GetUserProfile(1, 2);
            var ok = result as OkObjectResult;
            var profile = ok?.Value as UserProfileDto;

            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.IsFollowing, Is.True);
        }
    }
}