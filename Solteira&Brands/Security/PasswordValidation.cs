using System.Text.RegularExpressions;

namespace Solteira_Brands.Security
{
    /// <summary>
    /// Server-side password rules: 8–64 chars, 1 upper, 1 lower, 1 number, 1 special.
    /// Enforce on registration and any password change.
    /// </summary>
    public static class PasswordValidation
    {
        public const int MinLength = 8;
        public const int MaxLength = 64;

        private static readonly Regex Upper = new Regex(@"[A-Z]");
        private static readonly Regex Lower = new Regex(@"[a-z]");
        private static readonly Regex Number = new Regex(@"[0-9]");
        private static readonly Regex Special = new Regex(@"[!""#$%&'()*+,\-./:;<=>?@[\\\]^_`{|}~]");

        /// <summary>
        /// Returns true if password meets all rules.
        /// </summary>
        public static bool IsValid(string password, out string errorMessage)
        {
            errorMessage = null;
            if (string.IsNullOrEmpty(password))
            {
                errorMessage = "Password is required.";
                return false;
            }
            if (password.Length < MinLength)
            {
                errorMessage = $"Password must be at least {MinLength} characters.";
                return false;
            }
            if (password.Length > MaxLength)
            {
                errorMessage = $"Password must not exceed {MaxLength} characters.";
                return false;
            }
            if (!Upper.IsMatch(password))
            {
                errorMessage = "Password must contain at least one uppercase letter.";
                return false;
            }
            if (!Lower.IsMatch(password))
            {
                errorMessage = "Password must contain at least one lowercase letter.";
                return false;
            }
            if (!Number.IsMatch(password))
            {
                errorMessage = "Password must contain at least one number.";
                return false;
            }
            if (!Special.IsMatch(password))
            {
                errorMessage = "Password must contain at least one special character.";
                return false;
            }
            return true;
        }
    }
}
