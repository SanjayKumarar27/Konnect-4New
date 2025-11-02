using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Konnect_4New.Controllers
{
    public class BaseController : ControllerBase
    {
        /// <summary>
        /// Gets the current authenticated user's ID from JWT claims
        /// </summary>
        protected int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }

            return userId;
        }

        /// <summary>
        /// Gets the current authenticated user's role from JWT claims
        /// </summary>
        protected string GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
        }

        /// <summary>
        /// Gets the current authenticated user's username from JWT claims
        /// </summary>
        protected string GetCurrentUsername()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
        }

        /// <summary>
        /// Checks if the current user is an admin
        /// </summary>
        protected bool IsAdmin()
        {
            return GetCurrentUserRole() == "Admin";
        }

        /// <summary>
        /// Checks if the current user matches the specified userId
        /// </summary>
        protected bool IsCurrentUser(int userId)
        {
            return GetCurrentUserId() == userId;
        }
    }
}