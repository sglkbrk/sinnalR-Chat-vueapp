using System.Security.Cryptography;
using System.Text;
using ProductManagement.Application.Common;
using Xunit;

namespace ProductManagement.Tests
{
    public class PasswordHasherTests
    {
        [Fact]
        public void HashPassword_ShouldReturnValidPBKDF2String()
        {
            string password = "TestPassword123!";
            string hash = PasswordHasher.HashPassword(password);

            Assert.NotNull(hash);
            Assert.StartsWith("PBKDF2$100000$", hash);
        }

        [Fact]
        public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
        {
            string password = "SecretPassword456";
            string hash = PasswordHasher.HashPassword(password);

            bool isValid = PasswordHasher.VerifyPassword(password, hash, out bool needsRehash);

            Assert.True(isValid);
            Assert.False(needsRehash);
        }

        [Fact]
        public void VerifyPassword_WithWrongPassword_ShouldReturnFalse()
        {
            string password = "SecretPassword456";
            string hash = PasswordHasher.HashPassword(password);

            bool isValid = PasswordHasher.VerifyPassword("WrongPassword!", hash, out bool needsRehash);

            Assert.False(isValid);
            Assert.False(needsRehash);
        }

        [Fact]
        public void VerifyPassword_WithLegacyMD5_ShouldReturnTrueAndNeedsRehash()
        {
            string password = "MyOldPassword";
            // Create legacy MD5 hash
            using var md5 = MD5.Create();
            byte[] inputBytes = Encoding.UTF8.GetBytes(password);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            var sb = new StringBuilder();
            foreach (var b in hashBytes) sb.Append(b.ToString("X2"));
            string md5Hash = sb.ToString();

            bool isValid = PasswordHasher.VerifyPassword(password, md5Hash, out bool needsRehash);

            Assert.True(isValid);
            Assert.True(needsRehash);
        }
    }
}

