using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramSessionNotificationBot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "NotificationBotId",
                table: "TelegramSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TelegramSessions_NotificationBotId",
                table: "TelegramSessions",
                column: "NotificationBotId");

            migrationBuilder.AddForeignKey(
                name: "FK_TelegramSessions_TelegramBots_NotificationBotId",
                table: "TelegramSessions",
                column: "NotificationBotId",
                principalTable: "TelegramBots",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TelegramSessions_TelegramBots_NotificationBotId",
                table: "TelegramSessions");

            migrationBuilder.DropIndex(
                name: "IX_TelegramSessions_NotificationBotId",
                table: "TelegramSessions");

            migrationBuilder.DropColumn(
                name: "NotificationBotId",
                table: "TelegramSessions");
        }
    }
}
