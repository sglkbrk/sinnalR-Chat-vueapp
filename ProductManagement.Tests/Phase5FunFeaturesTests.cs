using Microsoft.EntityFrameworkCore;
using ProductManagement.Application.Services;
using ProductManagement.Domain.Models;
using ProductManagement.Infrastructure.Data;
using Xunit;

namespace ProductManagement.Tests
{
    public class Phase5FunFeaturesTests
    {
        private ApplicationDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task StartXoxGame_ShouldInitializeBoardAndTurn()
        {
            using var context = CreateInMemoryDbContext();
            var service = new MessageService(context);

            var gameMsg = await service.StartXoxGameAsync(1, 2, "Alice", "Bob");

            Assert.NotNull(gameMsg);
            Assert.Equal(messageType.Game, gameMsg.Type);
            Assert.Contains("Alice", gameMsg.Content);

            var state = System.Text.Json.JsonSerializer.Deserialize<ProductManagement.Application.DTO.XoxGameState>(gameMsg.MetadataJson!);
            Assert.NotNull(state);
            Assert.Equal("xox", state.GameType);
            Assert.Equal("X", state.Turn);
            Assert.Equal(1, state.PlayerX);
            Assert.Equal(2, state.PlayerO);
            Assert.All(state.Board, cell => Assert.Equal("", cell));
        }

        [Fact]
        public async Task MakeXoxMove_ValidMove_ShouldSwitchTurn()
        {
            using var context = CreateInMemoryDbContext();
            var service = new MessageService(context);

            var gameMsg = await service.StartXoxGameAsync(1, 2, "Alice", "Bob");

            // Alice (X) plays at cell 0
            var (updatedMsg, state) = await service.MakeXoxMoveAsync(gameMsg.Id, 1, 0);

            Assert.Equal("X", state.Board[0]);
            Assert.Equal("O", state.Turn);
            Assert.Equal("playing", state.Status);
            Assert.Null(state.Winner);
        }

        [Fact]
        public async Task MakeXoxMove_OutOfTurn_ShouldThrowInvalidOperationException()
        {
            using var context = CreateInMemoryDbContext();
            var service = new MessageService(context);

            var gameMsg = await service.StartXoxGameAsync(1, 2, "Alice", "Bob");

            // Bob (O) tries to play when it's X's turn
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.MakeXoxMoveAsync(gameMsg.Id, 2, 0));
        }

        [Fact]
        public async Task MakeXoxMove_CellAlreadyOccupied_ShouldThrowInvalidOperationException()
        {
            using var context = CreateInMemoryDbContext();
            var service = new MessageService(context);

            var gameMsg = await service.StartXoxGameAsync(1, 2, "Alice", "Bob");
            await service.MakeXoxMoveAsync(gameMsg.Id, 1, 0); // Alice plays 0

            // Bob tries to play at cell 0
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.MakeXoxMoveAsync(gameMsg.Id, 2, 0));
        }

        [Fact]
        public async Task MakeXoxMove_WinningRow_ShouldSetWinnerAndFinish()
        {
            using var context = CreateInMemoryDbContext();
            var service = new MessageService(context);

            var gameMsg = await service.StartXoxGameAsync(1, 2, "Alice", "Bob");

            // Alice: 0, 1, 2 (Top row)
            // Bob: 3, 4
            await service.MakeXoxMoveAsync(gameMsg.Id, 1, 0); // X at 0
            await service.MakeXoxMoveAsync(gameMsg.Id, 2, 3); // O at 3
            await service.MakeXoxMoveAsync(gameMsg.Id, 1, 1); // X at 1
            await service.MakeXoxMoveAsync(gameMsg.Id, 2, 4); // O at 4
            var (finalMsg, finalState) = await service.MakeXoxMoveAsync(gameMsg.Id, 1, 2); // X at 2 (wins!)

            Assert.Equal("finished", finalState.Status);
            Assert.Equal("X", finalState.Winner);
            Assert.NotNull(finalState.WinningLine);
            Assert.Equal(new int[] { 0, 1, 2 }, finalState.WinningLine);
            Assert.Contains("Alice kazandı", finalMsg.Content);
        }

        [Fact]
        public async Task ExpireMessage_ShouldMarkDeletedWithExpireContent()
        {
            using var context = CreateInMemoryDbContext();
            context.Users.Add(new Users { id = 1, Username = "test", Email = "t@t.com", Password = "p" });
            await context.SaveChangesAsync();

            var service = new MessageService(context);

            var msg = new Message
            {
                SenderId = 1,
                ReceiverId = 2,
                Content = "Gizli sır",
                ExpiresAt = DateTime.UtcNow.AddSeconds(10)
            };
            await service.AddMessageAsync(msg);

            var success = await service.ExpireMessageAsync(msg.Id);
            Assert.True(success);

            var reloaded = service.GetMessageById(msg.Id);
            Assert.NotNull(reloaded);
            Assert.True(reloaded.IsDeleted);
            Assert.Contains("süresi doldu", reloaded.Content);
        }

        [Fact]
        public async Task StartRpsGame_ShouldInitializeBestOfThreeState()
        {
            using var context = CreateInMemoryDbContext();
            var service = new MessageService(context);

            var gameMsg = await service.StartRpsGameAsync(1, 2, "Alice", "Bob");

            Assert.NotNull(gameMsg);
            Assert.Equal(messageType.Game, gameMsg.Type);

            var state = System.Text.Json.JsonSerializer.Deserialize<ProductManagement.Application.DTO.RpsGameState>(gameMsg.MetadataJson!);
            Assert.NotNull(state);
            Assert.Equal("rps", state.GameType);
            Assert.Equal(1, state.Player1Id);
            Assert.Equal(2, state.Player2Id);
            Assert.Equal(3, state.TargetScore);
            Assert.Equal("playing", state.Status);
        }

        [Fact]
        public async Task MakeRpsMove_ThreeRoundsWon_ShouldFinishGameWithWinner()
        {
            using var context = CreateInMemoryDbContext();
            var service = new MessageService(context);

            var gameMsg = await service.StartRpsGameAsync(1, 2, "Alice", "Bob");

            // Round 1: Alice (rock) beats Bob (scissors) -> 1-0
            await service.MakeRpsMoveAsync(gameMsg.Id, 1, "rock");
            var (_, stateR1) = await service.MakeRpsMoveAsync(gameMsg.Id, 2, "scissors");
            Assert.Equal(1, stateR1.Player1Score);
            Assert.Equal(0, stateR1.Player2Score);
            Assert.Equal(2, stateR1.Round);
            Assert.Null(stateR1.Player1Choice);
            Assert.Null(stateR1.Player2Choice);

            // Round 2: Alice (paper) beats Bob (rock) -> 2-0
            await service.MakeRpsMoveAsync(gameMsg.Id, 1, "paper");
            await service.MakeRpsMoveAsync(gameMsg.Id, 2, "rock");

            // Round 3: Alice (scissors) beats Bob (paper) -> 3-0 (Alice wins!)
            await service.MakeRpsMoveAsync(gameMsg.Id, 1, "scissors");
            var (finalMsg, finalState) = await service.MakeRpsMoveAsync(gameMsg.Id, 2, "paper");

            Assert.Equal("finished", finalState.Status);
            Assert.Equal("Alice", finalState.Winner);
            Assert.Equal(3, finalState.Player1Score);
            Assert.Contains("Alice", finalMsg.Content);
            Assert.Contains("kazandı", finalMsg.Content);
        }
    }
}
