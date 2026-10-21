using Microsoft.EntityFrameworkCore;
using ProductManagement.Application.Services;
using ProductManagement.Domain.Models;
using ProductManagement.Infrastructure.Data;
using Xunit;

namespace ProductManagement.Tests
{
    public class Phase4MessageServicesTests
    {
        private ApplicationDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task EditMessage_WhenValid_ShouldUpdateContentAndEditedAt()
        {
            using var context = CreateInMemoryDbContext();
            var msg = new Message
            {
                Id = 1,
                SenderId = 10,
                ReceiverId = 20,
                Content = "Merhaba",
                Timestamp = DateTime.UtcNow
            };
            context.Message.Add(msg);
            await context.SaveChangesAsync();

            var service = new MessageService(context);
            var updated = await service.EditMessageAsync(msg.Id, 10, "Merhaba güncellendi!");

            Assert.NotNull(updated);
            Assert.Equal("Merhaba güncellendi!", updated.Content);
            Assert.NotNull(updated.EditedAt);
        }

        [Fact]
        public async Task EditMessage_ByNonSender_ShouldThrowUnauthorizedAccessException()
        {
            using var context = CreateInMemoryDbContext();
            var msg = new Message
            {
                Id = 2,
                SenderId = 10,
                ReceiverId = 20,
                Content = "Gizli",
                Timestamp = DateTime.UtcNow
            };
            context.Message.Add(msg);
            await context.SaveChangesAsync();

            var service = new MessageService(context);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.EditMessageAsync(msg.Id, 20, "Hacker"));
        }

        [Fact]
        public async Task DeleteForMe_ShouldHideOnlyForCurrentUser()
        {
            using var context = CreateInMemoryDbContext();
            var msg1 = new Message
            {
                Id = 10,
                SenderId = 1,
                ReceiverId = 2,
                Content = "Mesaj 1",
                Timestamp = DateTime.UtcNow.AddMinutes(-5)
            };
            var msg2 = new Message
            {
                Id = 11,
                SenderId = 2,
                ReceiverId = 1,
                Content = "Mesaj 2",
                Timestamp = DateTime.UtcNow
            };
            context.Message.AddRange(msg1, msg2);
            await context.SaveChangesAsync();

            var service = new MessageService(context);

            // User 1 deletes msg1 for me
            await service.DeleteMessageForMeAsync(msg1.Id, 1);

            // For User 1, msg1 should be hidden
            var user1Messages = await service.GetMyMessagesAsync(1, 2);
            Assert.Single(user1Messages);
            Assert.Equal(msg2.Id, user1Messages[0].Id);

            // For User 2, both messages should still be visible
            var user2Messages = await service.GetMyMessagesAsync(2, 1);
            Assert.Equal(2, user2Messages.Count);
        }

        [Fact]
        public async Task DeleteForEveryone_ShouldMarkDeleted()
        {
            using var context = CreateInMemoryDbContext();
            var user5 = new Users { id = 5, Username = "user5", Email = "u5@test.com", Password = "p" };
            var user6 = new Users { id = 6, Username = "user6", Email = "u6@test.com", Password = "p" };
            context.Users.AddRange(user5, user6);

            var msg = new Message
            {
                SenderId = 5,
                ReceiverId = 6,
                Content = "Yanlış mesaj",
                Timestamp = DateTime.UtcNow
            };
            context.Message.Add(msg);
            await context.SaveChangesAsync();

            var service = new MessageService(context);
            var success = await service.DeleteMessageForEveryoneAsync(msg.Id, 5);

            Assert.True(success);
            var reloaded = service.GetMessageById(msg.Id);
            Assert.NotNull(reloaded);
            Assert.True(reloaded.IsDeleted);
            Assert.Equal("Bu mesaj silindi", reloaded.Content);
        }

        [Fact]
        public async Task ClearChat_ShouldHideAllMessagesForRequesterOnly()
        {
            using var context = CreateInMemoryDbContext();
            var msg1 = new Message { Id = 31, SenderId = 1, ReceiverId = 2, Content = "A" };
            var msg2 = new Message { Id = 32, SenderId = 2, ReceiverId = 1, Content = "B" };
            context.Message.AddRange(msg1, msg2);
            await context.SaveChangesAsync();

            var service = new MessageService(context);
            await service.ClearChatAsync(1, 2);

            var user1Messages = await service.GetMyMessagesAsync(1, 2);
            Assert.Empty(user1Messages);

            var user2Messages = await service.GetMyMessagesAsync(2, 1);
            Assert.Equal(2, user2Messages.Count);
        }

        [Fact]
        public async Task SetReaction_ToggleBehavior_ShouldWorkCorrectly()
        {
            using var context = CreateInMemoryDbContext();
            var msg = new Message { Id = 40, SenderId = 1, ReceiverId = 2, Content = "Komik" };
            context.Message.Add(msg);
            await context.SaveChangesAsync();

            var service = new MessageService(context);

            // Add reaction 👍
            var reactions = await service.SetReactionAsync(msg.Id, 1, "👍");
            Assert.True(reactions.ContainsKey("1"));
            Assert.Equal("👍", reactions["1"]);

            // Re-click 👍 -> should toggle off (remove)
            var reactionsAfterToggle = await service.SetReactionAsync(msg.Id, 1, "👍");
            Assert.False(reactionsAfterToggle.ContainsKey("1"));

            // Add ❤️
            var reactionsHeart = await service.SetReactionAsync(msg.Id, 1, "❤️");
            Assert.Equal("❤️", reactionsHeart["1"]);
        }

        [Fact]
        public async Task GetMyMessages_WithReply_ShouldIncludeReplySummary()
        {
            using var context = CreateInMemoryDbContext();
            var senderUser = new Users { id = 1, Username = "ahmet", Email = "ahmet@test.com", Password = "p" };
            var receiverUser = new Users { id = 2, Username = "mehmet", Email = "mehmet@test.com", Password = "p" };
            context.Users.AddRange(senderUser, receiverUser);

            var parentMsg = new Message
            {
                SenderId = senderUser.id,
                ReceiverId = receiverUser.id,
                Content = "Nasılsın?",
                Timestamp = DateTime.UtcNow.AddMinutes(-2)
            };
            context.Message.Add(parentMsg);
            await context.SaveChangesAsync();

            var replyMsg = new Message
            {
                SenderId = receiverUser.id,
                ReceiverId = senderUser.id,
                Content = "İyiyim teşekkürler!",
                ReplyToMessageId = parentMsg.Id,
                Timestamp = DateTime.UtcNow
            };
            context.Message.Add(replyMsg);
            await context.SaveChangesAsync();

            var service = new MessageService(context);
            var list = await service.GetMyMessagesAsync(receiverUser.id, senderUser.id);

            Assert.Equal(2, list.Count);
            var replyDto = list.FirstOrDefault(m => m.Id == replyMsg.Id);
            Assert.NotNull(replyDto);
            Assert.NotNull(replyDto.ReplyToMessage);
            Assert.Equal("ahmet", replyDto.ReplyToMessage.SenderName);
            Assert.Equal("Nasılsın?", replyDto.ReplyToMessage.Content);
        }
    }
}
