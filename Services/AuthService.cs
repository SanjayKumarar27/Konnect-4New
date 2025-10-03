using System.Security.Cryptography;
using System.Text;

namespace Konnect_4New.Services
{
    public static class AuthService
    {
        public static string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password cannot be empty or null.", nameof(password));
            }
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public static bool VerifyPassword(string enteredPassword, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(enteredPassword) || string.IsNullOrWhiteSpace(storedHash))
            {
                return false;
            }
            return BCrypt.Net.BCrypt.Verify(enteredPassword, storedHash);
        }
    }
}
