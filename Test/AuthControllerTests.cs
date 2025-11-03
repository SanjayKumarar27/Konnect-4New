// File: Konnect_4New.Tests/Controllers/AuthControllerTests.cs
using Konnect_4New.Controllers;
using Konnect_4New.Models;
using Konnect_4New.Models.Dtos;
using Konnect_4New.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Konnect_4New.Tests.Controllers
{
    [TestFixture]
    public class AuthControllerTests
    {
        private Konnect4Context _context = null!;
        private AuthController _controller = null!;
        private Mock<IJwtService> _jwtServiceMock = null!;

        [SetUp]
        public void Setup()
        {
            // ✅ In-memory EF Core database
            var options = new DbContextOptionsBuilder<Konnect4Context>()
                .UseInMemoryDatabase($"AuthDb_{Guid.NewGuid()}")
                .Options;

            _context = new Konnect4Context(options);

            // ✅ Mock JwtService
            _jwtServiceMock = new Mock<IJwtService>();
            _jwtServiceMock
                .Setup(s => s.GenerateToken(It.IsAny<User>()))
                .Returns("fake-jwt-token");

            _controller = new AuthController(_context, _jwtServiceMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        // ✅ TEST 1: Register Success
        [Test]
        public async Task Register_ValidData_ReturnsSuccess()
        {
            var dto = new RegisterDto
            {
                Username = "alice",
                Email = "alice@test.com",
                Password = "pass123",
                FullName = "Alice"
            };

            var result = await _controller.Register(dto);
            var ok = result as OkObjectResult;

            Assert.That(ok, Is.Not.Null);
            Assert.That(ok.Value, Is.Not.Null);

            var message = ok.Value?.GetType().GetProperty("message")?.GetValue(ok.Value);
            Assert.That(message, Is.EqualTo("Registration successful!"));
        }

        // ✅ TEST 2: Register Duplicate Email
        [Test]
        public async Task Register_DuplicateEmail_ReturnsBadRequest()
        {
            await _context.Users.AddAsync(new User
            {
                Username = "old",
                Email = "taken@test.com",
                PasswordHash = "any"
            });
            await _context.SaveChangesAsync();

            var dto = new RegisterDto
            {
                Username = "newuser",
                Email = "taken@test.com",
                Password = "p"
            };

            var result = await _controller.Register(dto);
            var bad = result as BadRequestObjectResult;

            Assert.That(bad?.Value, Is.EqualTo("Email or Username already exists."));
        }

        // ✅ TEST 3: Login Success
        [Test]
        public async Task Login_ValidCredentials_ReturnsUserInfoAndToken()
        {
            // Insert a known user
            var user = new User
            {
                UserId = 1,
                Username = "bob",
                Email = "bob@test.com",
                FullName = "Bob",
                PasswordHash = AuthService.HashPassword("secret")
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var dto = new LoginDto { Email = "bob@test.com", Password = "secret" };
            var result = await _controller.Login(dto);
            var ok = result as OkObjectResult;

            Assert.That(ok, Is.Not.Null);

            var message = ok.Value?.GetType().GetProperty("message")?.GetValue(ok.Value);
            var token = ok.Value?.GetType().GetProperty("token")?.GetValue(ok.Value);
            var userObj = ok.Value?.GetType().GetProperty("user")?.GetValue(ok.Value);

            Assert.That(message, Is.EqualTo("Login successful!"));
            Assert.That(token, Is.EqualTo("fake-jwt-token"));
            Assert.That(userObj, Is.Not.Null);

            var userId = userObj?.GetType().GetProperty("UserId")?.GetValue(userObj);
            Assert.That(userId, Is.EqualTo(1));
        }

        // ✅ TEST 4: Login Invalid Credentials
        [Test]
        public async Task Login_InvalidCredentials_ReturnsUnauthorized()
        {
            var dto = new LoginDto { Email = "notfound@test.com", Password = "wrong" };
            var result = await _controller.Login(dto);

            var unauthorized = result as UnauthorizedObjectResult;
            Assert.That(unauthorized, Is.Not.Null);
            Assert.That(unauthorized!.Value, Is.EqualTo("Invalid credentials."));
        }
    }
}
