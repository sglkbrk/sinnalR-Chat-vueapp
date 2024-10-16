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

            // Mesajların ilişkilerini tanımla
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

            builder.Entity<FbToken>()
                .HasOne(m => m.User)
                .WithOne()
                .HasForeignKey<FbToken>(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);


        }
    }
}
