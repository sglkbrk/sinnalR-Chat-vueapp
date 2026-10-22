using ProductManagement.Infrastructure.Data;
using ProductManagement.Domain.Models;
using ProductManagement.Application.DTO;
using ProductManagement.Application.Common;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ProductManagement.Application.Services
{
    public class UserAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public UserAuthService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public void Register(Users model)
        {
            var trimmedUsername = model.Username?.Trim() ?? string.Empty;
            var trimmedEmail = model.Email?.Trim().ToLowerInvariant() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(trimmedUsername) || string.IsNullOrWhiteSpace(trimmedEmail) || string.IsNullOrWhiteSpace(model.Password))
            {
                throw new AppException("Lütfen kullanıcı adı, e-posta ve şifreyi eksiksiz girin.", HttpStatusCode.BadRequest, "VALIDATION_ERROR");
            }

            if (model.Password.Length < 6)
            {
                throw new AppException("Şifre en az 6 karakter olmalıdır.", HttpStatusCode.BadRequest, "PASSWORD_TOO_SHORT");
            }

            bool exists = _context.Users.Any(x => x.Email.ToLower() == trimmedEmail || x.Username.ToLower() == trimmedUsername.ToLower());
            if (exists)
            {
                throw new AppException("Bu kullanıcı adı veya e-posta adresi zaten kayıtlı.", HttpStatusCode.Conflict, "USER_ALREADY_EXISTS");
            }

            model.Username = trimmedUsername;
            model.Email = trimmedEmail;
            model.Password = PasswordHasher.HashPassword(model.Password);
            model.CreatedAt = DateTime.UtcNow;

            _context.Users.Add(model);
            _context.SaveChanges();

            if (string.IsNullOrEmpty(model.UserCode))
            {
                model.UserCode = model.id.ToString("D4");
                _context.SaveChanges();
            }
        }

        public async Task<(string token, Users user)> Login(LoginModel model)
        {
            var trimmedIdentifier = model.Username?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmedIdentifier) || string.IsNullOrWhiteSpace(model.Password))
            {
                throw new AppException("Kullanıcı adı ve şifre gereklidir.", HttpStatusCode.BadRequest, "VALIDATION_ERROR");
            }

            var user = await _context.Users.SingleOrDefaultAsync(x =>
                x.Username.ToLower() == trimmedIdentifier.ToLower() ||
                x.Email.ToLower() == trimmedIdentifier.ToLower());

            if (user == null)
            {
                throw new AppException("Kullanıcı adı veya şifre hatalı.", HttpStatusCode.Unauthorized, "INVALID_CREDENTIALS");
            }

            if (!PasswordHasher.VerifyPassword(model.Password, user.Password, out bool needsRehash))
            {
                throw new AppException("Kullanıcı adı veya şifre hatalı.", HttpStatusCode.Unauthorized, "INVALID_CREDENTIALS");
            }

            if (needsRehash)
            {
                user.Password = PasswordHasher.HashPassword(model.Password);
                await _context.SaveChangesAsync();
            }

            string token = GenerateJwtToken(user);
            return (token, user);
        }

        public List<UserDto> GetUsersAsync(string currentUserId)
        {
            var users = _context.Users
            .Where(u => u.id.ToString() != currentUserId)
            .Select(u => new UserDto
            {
                Id = u.id,
                UserName = u.Username,
                Email = u.Email,
                AvatarUrl = u.AvatarUrl,
                StatusMessage = u.StatusMessage,
                LastSeenAt = u.LastSeenAt,
                LastMessage = _context.Message
                    .Where(m => (m.SenderId == u.id && m.ReceiverId.ToString() == currentUserId && !m.IsDeletedForReceiver) ||
                                (m.ReceiverId == u.id && m.SenderId.ToString() == currentUserId && !m.IsDeletedForSender))
                    .OrderByDescending(m => m.Timestamp)
                    .Select(m => new MessageDto
                    {
                        Content = m.Content,
                        Timestamp = m.Timestamp
                    })
                    .FirstOrDefault()
            }).ToList();

            return users;
        }

        public List<Users> GetUsers(string currentUserId)
        {

            var users = _context.Users
                .Where(u => u.id.ToString() != currentUserId).ToList();
            return users;
        }
        private string GenerateJwtToken(Users user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.id.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("name", user.Username)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "default_jwt_secret_key_1234567890_min_32_chars"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(30),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

}