using System.Security.Cryptography;
using System.Text;

namespace ProductManagement.Application.Common
{
    public static class PasswordHasher
    {
        private const int SaltSize = 16; // 128 bit
        private const int KeySize = 32;  // 256 bit
        private const int Iterations = 100_000;
        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

        public static string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Şifre boş olamaz.", nameof(password));

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                Iterations,
                Algorithm,
                KeySize
            );

            return $"PBKDF2${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public static bool VerifyPassword(string password, string storedHash, out bool needsRehash)
        {
            needsRehash = false;
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
                return false;

            if (storedHash.StartsWith("PBKDF2$"))
            {
                var parts = storedHash.Split('$');
                if (parts.Length != 4) return false;

                if (!int.TryParse(parts[1], out int iterations)) return false;
                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] expectedHash = Convert.FromBase64String(parts[3]);

                byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                    Encoding.UTF8.GetBytes(password),
                    salt,
                    iterations,
                    Algorithm,
                    expectedHash.Length
                );

                return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
            }

            // Geriye dönük uyumluluk: Eski MD5 kontrolü (32 karakter hex)
            string md5Hash = HashPasswordMD5(password);
            if (string.Equals(md5Hash, storedHash, StringComparison.OrdinalIgnoreCase))
            {
                needsRehash = true; // İlk başarılı girişte yeni PBKDF2'ye yükseltilsin
                return true;
            }

            return false;
        }

        private static string HashPasswordMD5(string password)
        {
            using var md5 = MD5.Create();
            byte[] inputBytes = Encoding.UTF8.GetBytes(password);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            var sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }
}

