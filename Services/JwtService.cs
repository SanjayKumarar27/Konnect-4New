using System;
﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Konnect_4New.Models;

namespace Konnect_4New.Services
{
    public interface IJwtService
    {
        string GenerateToken(User user);
        int? GetUserIdFromToken(string token);
        string? GetUserRoleFromToken(string token);
        ClaimsPrincipal? ValidateToken(string token);
    }

    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;
        private readonly string _secretKey;
        private readonly int _expirationMinutes;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _secretKey = _configuration["Jwt:SecretKey"]
                          ?? throw new ArgumentNullException("Jwt:SecretKey is not configured");
            _expirationMinutes = int.TryParse(_configuration["Jwt:ExpirationMinutes"], out var expiration)
                ? expiration
                : 60;  // Default to 60 minutes if not specified
        }

        /// <summary>
        /// Generates a JWT token for an authenticated user.
        /// </summary>
        public string GenerateToken(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            var claims = CreateClaims(user);

            var credentials = CreateSigningCredentials();
            var token = CreateJwtSecurityToken(claims, credentials);

            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// Validates the provided JWT token and returns the ClaimsPrincipal.
        /// </summary>
        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"] ?? "Konnect4",
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"] ?? "Konnect4Users",
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero // Ensures no skew is allowed for token expiration validation
                };

                return tokenHandler.ValidateToken(token, validationParameters, out _);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Token validation failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Extracts the user ID from the JWT token.
        /// </summary>
        public int? GetUserIdFromToken(string token)
        {
            var principal = ValidateToken(token);
            var userIdClaim = principal?.FindFirst(ClaimTypes.NameIdentifier);

            return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId)
                ? userId
                : (int?)null;
        }

        /// <summary>
        /// Extracts the user role from the JWT token.
        /// </summary>
        public string? GetUserRoleFromToken(string token)
        {
            var principal = ValidateToken(token);
            return principal?.FindFirst(ClaimTypes.Role)?.Value;
        }

        // Helper method to create the claims for the user
        private List<Claim> CreateClaims(User user)
        {
            return new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("FullName", user.FullName ?? user.Username),
                new Claim(ClaimTypes.Role, user.Role ?? "User")
            };
        }

        // Helper method to create signing credentials
        private SigningCredentials CreateSigningCredentials()
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            return new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        }

        // Helper method to create the JWT token
        private JwtSecurityToken CreateJwtSecurityToken(List<Claim> claims, SigningCredentials credentials)
        {
            return new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "Konnect4",
                audience: _configuration["Jwt:Audience"] ?? "Konnect4Users",
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_expirationMinutes)
                
            );
        }
    }
}
       
