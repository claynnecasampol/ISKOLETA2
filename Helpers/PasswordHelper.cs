using Microsoft.AspNetCore.Identity;

namespace FITNSS.Helpers
{
    public static class PasswordHelper
    {
        private static readonly PasswordHasher<object> _hasher = new PasswordHasher<object>();

        public static string HashPassword(string password)
        {
            return _hasher.HashPassword(new object(), password); // Hashes the password
        }

        public static bool VerifyPassword(string hashedPassword, string password)
        {
            // Verifies if the hashed password matches the entered password
            return _hasher.VerifyHashedPassword(new object(), hashedPassword, password) == PasswordVerificationResult.Success;
        }
    }
}
