using ProductManagement.Infrastructure.Data;

using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
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

            // Eğer model geçerliyse, parolayı hash'leyip kaydetme işlemi yapar
            Users users = _context.Users.FirstOrDefault(x => x.Email == model.Email || x.Username == model.Username);
            if (users != null)
            {
                throw new Exception("Bu kullanıcı zaten kayıtlı");
            }
            else
            {
                model.Password = HashPasswordMD5(model.Password);
                _context.Users.Add(model);
                _context.SaveChanges();
            }



        }
        public async Task<string> Login(LoginModel model)
        {
            var user = await _context.Users.SingleOrDefaultAsync(x => x.Username == model.Username || x.Email == model.Username);

            if (user == null)
                throw new KeyNotFoundException("User not found.");

            if (user.Password != HashPasswordMD5(model.Password))
                throw new UnauthorizedAccessException("Invalid password.");

            return GenerateJwtToken(user);

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
                LastMessage = _context.Message
                    .Where(m => (m.SenderId == u.id && m.ReceiverId.ToString() == currentUserId) ||
                                (m.ReceiverId == u.id && m.SenderId.ToString() == currentUserId))
                    .OrderByDescending(m => m.Timestamp)
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
        private static string HashPasswordMD5(string password)
        {

            if (password == null)
            {
                throw new ArgumentNullException("Password cannot be null");
            }
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.UTF8.GetBytes(password);
                var hashBytes = md5.ComputeHash(inputBytes);
                var sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2")); // Hex formatında temsil
                }
                return sb.ToString();
            }
        }
        private string GenerateJwtToken(Users user)
        {
            var claims = new[]
           {
            new Claim(JwtRegisteredClaimNames.Sub, user.id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("name", user.Username)
        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                   issuer: _configuration["Jwt:Issuer"],
                   audience: _configuration["Jwt:Audience"],
                   claims: claims,
                   expires: DateTime.Now.AddDays(30),
                   signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

}