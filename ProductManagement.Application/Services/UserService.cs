using ProductManagement.Infrastructure.Data;
using ProductManagement.Domain.Models;
using ProductManagement.Application.DTO;
using ProductManagement.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace ProductManagement.Application.Services
{
    public class UserService
    {
        private readonly ApplicationDbContext _context;
        private readonly MinioFileService _minioFileService;

        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif"
        };

        public UserService(ApplicationDbContext context, MinioFileService minioFileService)
        {
            _context = context;
            _minioFileService = minioFileService;
        }

        public async Task<UserProfileDto> GetProfileAsync(int userId)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.id == userId);
            if (user == null)
            {
                throw new AppException("Kullanıcı bulunamadı.", HttpStatusCode.NotFound, "USER_NOT_FOUND");
            }

            return new UserProfileDto
            {
                Id = user.id,
                Username = user.Username,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                StatusMessage = user.StatusMessage,
                UserCode = user.UserCode ?? $"#{user.id:D4}",
                LastSeenAt = user.LastSeenAt,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task UpdateProfileAsync(int userId, UpdateProfileRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.id == userId);
            if (user == null)
            {
                throw new AppException("Kullanıcı bulunamadı.", HttpStatusCode.NotFound, "USER_NOT_FOUND");
            }

            if (request.StatusMessage != null)
            {
                user.StatusMessage = request.StatusMessage.Trim();
            }

            await _context.SaveChangesAsync();
        }

        public async Task<string> UpdateAvatarAsync(int userId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new AppException("Geçerli bir resim dosyası seçilmedi.", HttpStatusCode.BadRequest, "EMPTY_FILE");
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                throw new AppException("Profil resmi 5MB'dan büyük olamaz.", HttpStatusCode.BadRequest, "FILE_TOO_LARGE");
            }

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(extension) || !AllowedImageExtensions.Contains(extension))
            {
                throw new AppException("Sadece JPG, PNG, WEBP veya GIF formatında resim yükleyebilirsiniz.", HttpStatusCode.BadRequest, "INVALID_FILE_TYPE");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.id == userId);
            if (user == null)
            {
                throw new AppException("Kullanıcı bulunamadı.", HttpStatusCode.NotFound, "USER_NOT_FOUND");
            }

            // Eski avatarı MinIO'dan sil
            if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
            {
                await _minioFileService.DeleteFileAsync(user.AvatarUrl);
            }

            // Yeni avatarı MinIO'ya yükle
            using var stream = file.OpenReadStream();
            var key = await _minioFileService.UploadStreamAsync(stream, file.FileName, file.ContentType);

            user.AvatarUrl = key;
            await _context.SaveChangesAsync();

            return key;
        }

        public async Task RemoveAvatarAsync(int userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.id == userId);
            if (user == null)
            {
                throw new AppException("Kullanıcı bulunamadı.", HttpStatusCode.NotFound, "USER_NOT_FOUND");
            }

            if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
            {
                await _minioFileService.DeleteFileAsync(user.AvatarUrl);
                user.AvatarUrl = null;
                await _context.SaveChangesAsync();
            }
        }
    }
}

