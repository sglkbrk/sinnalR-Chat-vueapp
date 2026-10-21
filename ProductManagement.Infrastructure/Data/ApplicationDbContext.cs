using Microsoft.EntityFrameworkCore;
using ProductManagement.Domain.Models;

namespace ProductManagement.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Message> Message { get; set; }
        public DbSet<Users> Users { get; set; }
        public DbSet<ChatRoom> ChatRoom { get; set; }
        public DbSet<FbToken> FbToken { get; set; }
        public DbSet<FriendRequest> FriendRequest { get; set; }
        public DbSet<Friend> Friend { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Users indeksleri
            builder.Entity<Users>()
                .HasIndex(u => u.Username)
                .IsUnique();

            builder.Entity<Users>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Mesaj ilişkileri ve indeksleri
            builder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Message>()
                .HasOne(m => m.ReplyToMessage)
                .WithMany()
                .HasForeignKey(m => m.ReplyToMessageId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Message>()
                .HasIndex(m => new { m.SenderId, m.ReceiverId, m.Timestamp });

            // Arkadaşlık ilişkileri ve indeksleri
            builder.Entity<Friend>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Friend>()
                .HasOne(f => f.FriendUser)
                .WithMany()
                .HasForeignKey(f => f.FriendUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Friend>()
                .HasIndex(f => new { f.UserId, f.FriendUserId })
                .IsUnique();

            // Arkadaşlık istekleri ilişkileri
            builder.Entity<FriendRequest>()
                .HasOne(r => r.Sender)
                .WithMany()
                .HasForeignKey(r => r.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FriendRequest>()
                .HasOne(r => r.Receiver)
                .WithMany()
                .HasForeignKey(r => r.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FriendRequest>()
                .HasIndex(r => new { r.SenderId, r.ReceiverId });

            // Cihaz push tokenları (Çoklu cihaz desteği 1:N)
            builder.Entity<FbToken>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<FbToken>()
                .HasIndex(t => t.Token);
        }
    }
}
