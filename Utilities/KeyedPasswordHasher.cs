using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace LogiTrack.Utilities
{
    // PBKDF2-based hasher that incorporates an application 'pepper' (passwordKey)
    public class KeyedPasswordHasher<TUser> : IPasswordHasher<TUser> where TUser : class
    {
        private const int SaltSize = 16; // 128 bit
        private const int SubkeySize = 32; // 256 bit
        private const int Iterations = 100_000;
        private readonly string _passwordKey; // pepper

        public KeyedPasswordHasher(string passwordKey)
        {
            _passwordKey = passwordKey ?? string.Empty;
        }

        public string HashPassword(TUser user, string password)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));

            // Generate a random salt
            byte[] salt = new byte[SaltSize];
            RandomNumberGenerator.Fill(salt);

            // Derive subkey using PBKDF2 over password+pepper
            var combined = password + _passwordKey;
            using var deriveBytes = new Rfc2898DeriveBytes(combined, salt, Iterations, HashAlgorithmName.SHA256);
            var subkey = deriveBytes.GetBytes(SubkeySize);

            // Format: {iterations}.{saltBase64}.{subkeyBase64}
            return string.Join('.', Iterations, Convert.ToBase64String(salt), Convert.ToBase64String(subkey));
        }

        public PasswordVerificationResult VerifyHashedPassword(TUser user, string hashedPassword, string providedPassword)
        {
            if (hashedPassword == null) throw new ArgumentNullException(nameof(hashedPassword));
            if (providedPassword == null) throw new ArgumentNullException(nameof(providedPassword));

            var parts = hashedPassword.Split('.');
            if (parts.Length != 3) return PasswordVerificationResult.Failed;

            if (!int.TryParse(parts[0], out int iterations)) return PasswordVerificationResult.Failed;
            var salt = Convert.FromBase64String(parts[1]);
            var expectedSubkey = Convert.FromBase64String(parts[2]);

            var combined = providedPassword + _passwordKey;
            using var deriveBytes = new Rfc2898DeriveBytes(combined, salt, iterations, HashAlgorithmName.SHA256);
            var actualSubkey = deriveBytes.GetBytes(expectedSubkey.Length);

            // Use a time-constant comparison
            if (CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey))
            {
                return PasswordVerificationResult.Success;
            }

            return PasswordVerificationResult.Failed;
        }
    }
}
