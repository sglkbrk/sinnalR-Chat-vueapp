using Microsoft.EntityFrameworkCore;
using ProductManagement.Application.Common;
using ProductManagement.Application.DTO;
using ProductManagement.Application.Services;
using ProductManagement.Domain.Models;
using ProductManagement.Infrastructure.Data;
using Xunit;

namespace ProductManagement.Tests
{
    public class Phase2ServicesTests
    {
        private ApplicationDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public void SendFriendRequest_ToSelf_ShouldThrowAppException()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var user1 = new Users { id = 1, Username = "alice", Email = "alice@test.com", Password = "hash" };
            context.Users.Add(user1);
            context.SaveChanges();

            var friendService = new FriendService(context);
            var service = new FriendRequestService(context, friendService);

            // Act & Assert
            var ex = Assert.Throws<AppException>(() => service.SendFriendRequest(1, null, 1));
            Assert.Contains("Kendinize", ex.Message);
        }

        [Fact]
        public void SendFriendRequest_DuplicatePending_ShouldThrowAppException()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var user1 = new Users { id = 1, Username = "alice", Email = "alice@test.com", Password = "hash" };
            var user2 = new Users { id = 2, Username = "bob", Email = "bob@test.com", Password = "hash" };
            context.Users.AddRange(user1, user2);
            context.SaveChanges();

            var friendService = new FriendService(context);
            var service = new FriendRequestService(context, friendService);

            // Act: First request succeeds
            service.SendFriendRequest(1, null, 2);

            // Assert: Second identical request throws
            var ex = Assert.Throws<AppException>(() => service.SendFriendRequest(1, null, 2));
            Assert.Contains("bekleyen", ex.Message);
        }

        [Fact]
        public void SearchUsers_ShouldReturnCorrectFriendshipStatuses()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var user1 = new Users { id = 1, Username = "alice", Email = "alice@test.com", Password = "hash" };
            var user2 = new Users { id = 2, Username = "bob", Email = "bob@test.com", Password = "hash", UserCode = "0002" };
            var user3 = new Users { id = 3, Username = "charlie", Email = "charlie@test.com", Password = "hash", UserCode = "0003" };
            var user4 = new Users { id = 4, Username = "david", Email = "david@test.com", Password = "hash", UserCode = "0004" };
            context.Users.AddRange(user1, user2, user3, user4);

            // user1 is friend with user2
            context.Friend.Add(new Friend { UserId = 1, FriendUserId = 2 });
            context.Friend.Add(new Friend { UserId = 2, FriendUserId = 1 });

            // user1 sent pending request to user3
            context.FriendRequest.Add(new FriendRequest { SenderId = 1, ReceiverId = 3, Status = FriendRequestStatus.Pending });

            // user4 sent pending request to user1
            context.FriendRequest.Add(new FriendRequest { SenderId = 4, ReceiverId = 1, Status = FriendRequestStatus.Pending });

            context.SaveChanges();

            var friendService = new FriendService(context);
            var service = new FriendRequestService(context, friendService);

            // Act
            var results = service.SearchUsers("o", 1); // bob matches "o"

            // Assert
            var bob = results.FirstOrDefault(r => r.Id == 2);
            Assert.NotNull(bob);
            Assert.True(bob.IsFriend);

            var charlieResults = service.SearchUsers("charlie", 1);
            var charlie = charlieResults.FirstOrDefault(r => r.Id == 3);
            Assert.NotNull(charlie);
            Assert.False(charlie.IsFriend);
            Assert.True(charlie.HasPendingOutgoing);

            var davidResults = service.SearchUsers("david", 1);
            var david = davidResults.FirstOrDefault(r => r.Id == 4);
            Assert.NotNull(david);
            Assert.True(david.HasPendingIncoming);
        }

        [Fact]
        public void AcceptFriendRequest_ShouldCreateBidirectionalFriendship()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var user1 = new Users { id = 1, Username = "alice", Email = "alice@test.com", Password = "hash" };
            var user2 = new Users { id = 2, Username = "bob", Email = "bob@test.com", Password = "hash" };
            context.Users.AddRange(user1, user2);

            var request = new FriendRequest { Id = 10, SenderId = 1, ReceiverId = 2, Status = FriendRequestStatus.Pending };
            context.FriendRequest.Add(request);
            context.SaveChanges();

            var friendService = new FriendService(context);
            var service = new FriendRequestService(context, friendService);

            // Act: User 2 accepts
            service.AcceptAndRejectFriendRequest(10, FriendRequestStatus.Accepted, 2);

            // Assert
            Assert.True(friendService.IsFriend(1, 2));
            Assert.True(friendService.IsFriend(2, 1));
            Assert.Equal(FriendRequestStatus.Accepted, request.Status);
        }

        [Fact]
        public void UpdateFriendSettings_ShouldUpdatePinnedAndMutedFlags()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var user1 = new Users { id = 1, Username = "alice", Email = "alice@test.com", Password = "hash" };
            var user2 = new Users { id = 2, Username = "bob", Email = "bob@test.com", Password = "hash" };
            context.Users.AddRange(user1, user2);

            var friendship = new Friend { UserId = 1, FriendUserId = 2, IsPinned = false, IsMuted = false };
            context.Friend.Add(friendship);
            context.SaveChanges();

            var friendService = new FriendService(context);

            // Act: Alice pins Bob
            friendService.UpdateFriendSettings(1, 2, isPinned: true, isMuted: true);

            // Assert
            var updated = context.Friend.First(f => f.UserId == 1 && f.FriendUserId == 2);
            Assert.True(updated.IsPinned);
            Assert.True(updated.IsMuted);
        }

        [Fact]
        public async Task UserService_GetAndModifyProfile_ShouldSucceed()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var user = new Users
            {
                id = 5,
                Username = "eve",
                Email = "eve@test.com",
                Password = "hash",
                StatusMessage = "Initial status",
                UserCode = "0005"
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var userService = new UserService(context, null!);

            // Act: Get Profile
            var profile = await userService.GetProfileAsync(5);
            Assert.Equal("eve", profile.Username);
            Assert.Equal("Initial status", profile.StatusMessage);

            // Act: Update Profile Status
            await userService.UpdateProfileAsync(5, new UpdateProfileRequest { StatusMessage = "New status" });

            // Assert
            var updatedProfile = await userService.GetProfileAsync(5);
            Assert.Equal("New status", updatedProfile.StatusMessage);
        }
    }
}
