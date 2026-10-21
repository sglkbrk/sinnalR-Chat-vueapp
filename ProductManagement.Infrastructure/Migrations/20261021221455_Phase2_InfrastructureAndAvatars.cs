using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase2_InfrastructureAndAvatars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Friend_Users_FriendUserId",
                table: "Friend");

            migrationBuilder.DropForeignKey(
                name: "FK_FriendRequest_Users_ReceiverId",
                table: "FriendRequest");

            migrationBuilder.DropForeignKey(
                name: "FK_FriendRequest_Users_SenderId",
                table: "FriendRequest");

            migrationBuilder.DropIndex(
                name: "IX_Message_SenderId",
                table: "Message");

            migrationBuilder.DropIndex(
                name: "IX_FriendRequest_SenderId",
                table: "FriendRequest");

            migrationBuilder.DropIndex(
                name: "IX_Friend_UserId",
                table: "Friend");

            migrationBuilder.DropIndex(
                name: "IX_FbToken_UserId",
                table: "FbToken");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Users",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                table: "Users",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeenAt",
                table: "Users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatusMessage",
                table: "Users",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "UserCode",
                table: "Users",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "EditedAt",
                table: "Message",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "Message",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetadataJson",
                table: "Message",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "ReplyToMessageId",
                table: "Message",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Friend",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsMuted",
                table: "Friend",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPinned",
                table: "Friend",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "FbToken",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DeviceInfo",
                table: "FbToken",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedAt",
                table: "FbToken",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Message_ReplyToMessageId",
                table: "Message",
                column: "ReplyToMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_Message_SenderId_ReceiverId_Timestamp",
                table: "Message",
                columns: new[] { "SenderId", "ReceiverId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_FriendRequest_SenderId_ReceiverId",
                table: "FriendRequest",
                columns: new[] { "SenderId", "ReceiverId" });

            migrationBuilder.CreateIndex(
                name: "IX_Friend_UserId_FriendUserId",
                table: "Friend",
                columns: new[] { "UserId", "FriendUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FbToken_Token",
                table: "FbToken",
                column: "Token");

            migrationBuilder.CreateIndex(
                name: "IX_FbToken_UserId",
                table: "FbToken",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Friend_Users_FriendUserId",
                table: "Friend",
                column: "FriendUserId",
                principalTable: "Users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FriendRequest_Users_ReceiverId",
                table: "FriendRequest",
                column: "ReceiverId",
                principalTable: "Users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FriendRequest_Users_SenderId",
                table: "FriendRequest",
                column: "SenderId",
                principalTable: "Users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Message_Message_ReplyToMessageId",
                table: "Message",
                column: "ReplyToMessageId",
                principalTable: "Message",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Friend_Users_FriendUserId",
                table: "Friend");

            migrationBuilder.DropForeignKey(
                name: "FK_FriendRequest_Users_ReceiverId",
                table: "FriendRequest");

            migrationBuilder.DropForeignKey(
                name: "FK_FriendRequest_Users_SenderId",
                table: "FriendRequest");

            migrationBuilder.DropForeignKey(
                name: "FK_Message_Message_ReplyToMessageId",
                table: "Message");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Username",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Message_ReplyToMessageId",
                table: "Message");

            migrationBuilder.DropIndex(
                name: "IX_Message_SenderId_ReceiverId_Timestamp",
                table: "Message");

            migrationBuilder.DropIndex(
                name: "IX_FriendRequest_SenderId_ReceiverId",
                table: "FriendRequest");

            migrationBuilder.DropIndex(
                name: "IX_Friend_UserId_FriendUserId",
                table: "Friend");

            migrationBuilder.DropIndex(
                name: "IX_FbToken_Token",
                table: "FbToken");

            migrationBuilder.DropIndex(
                name: "IX_FbToken_UserId",
                table: "FbToken");

            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastSeenAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StatusMessage",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UserCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EditedAt",
                table: "Message");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "Message");

            migrationBuilder.DropColumn(
                name: "MetadataJson",
                table: "Message");

            migrationBuilder.DropColumn(
                name: "ReplyToMessageId",
                table: "Message");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Friend");

            migrationBuilder.DropColumn(
                name: "IsMuted",
                table: "Friend");

            migrationBuilder.DropColumn(
                name: "IsPinned",
                table: "Friend");

            migrationBuilder.DropColumn(
                name: "DeviceInfo",
                table: "FbToken");

            migrationBuilder.DropColumn(
                name: "LastUpdatedAt",
                table: "FbToken");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Users",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "FbToken",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Message_SenderId",
                table: "Message",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_FriendRequest_SenderId",
                table: "FriendRequest",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Friend_UserId",
                table: "Friend",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FbToken_UserId",
                table: "FbToken",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Friend_Users_FriendUserId",
                table: "Friend",
                column: "FriendUserId",
                principalTable: "Users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FriendRequest_Users_ReceiverId",
                table: "FriendRequest",
                column: "ReceiverId",
                principalTable: "Users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FriendRequest_Users_SenderId",
                table: "FriendRequest",
                column: "SenderId",
                principalTable: "Users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
