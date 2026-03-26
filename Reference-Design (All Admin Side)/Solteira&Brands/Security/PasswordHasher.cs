using System;
using System.Security.Cryptography;
using System.Text;

namespace Solteira_Brands.Security
{
    /// <summary>
    /// PBKDF2 password hashing: 16-byte salt, 10,000 iterations, 32-byte hash, Base64 storage.
    /// Do not store plain passwords. Use HashPassword when registering, Verify when logging in.
    /// </summary>
    public static class PasswordHasher
    {
        private const int SaltSizeBytes = 16;
        private const int HashSizeBytes = 32;
        private const int IterationCount = 10000;

        /// <summary>
        /// Hash a password with a new random salt. Returns (hashBase64, saltBase64).
        /// </summary>
        public static (string HashBase64, string SaltBase64) HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be empty.", nameof(password));

            byte[] salt = new byte[SaltSizeBytes];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(salt);

            byte[] hash = ComputeHash(password, salt);
            return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
        }

        /// <summary>
        /// Verify a password against stored hash and salt.
        /// </summary>
        public static bool Verify(string password, string storedHashBase64, string storedSaltBase64)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHashBase64) || string.IsNullOrEmpty(storedSaltBase64))
                return false;

            byte[] salt;
            try { salt = Convert.FromBase64String(storedSaltBase64); }
            catch { return false; }

            byte[] expectedHash;
            try { expectedHash = Convert.FromBase64String(storedHashBase64); }
            catch { return false; }

            if (salt.Length != SaltSizeBytes || expectedHash.Length != HashSizeBytes)
                return false;

            byte[] actualHash = ComputeHash(password, salt);
            return FixedTimeEquals(expectedHash, actualHash);
        }

        /// <summary>Constant-time byte comparison for .NET Framework (no CryptographicOperations in 4.8).</summary>
        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }

        private static byte[] ComputeHash(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(
                Encoding.UTF8.GetBytes(password),
                salt,
                IterationCount))
            {
                return pbkdf2.GetBytes(HashSizeBytes);
            }
        }
    }
}
