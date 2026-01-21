using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TelegramSessionId",
                table: "ChannelParsingParameters",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TelegramSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiId = table.Column<string>(type: "text", nullable: false),
                    ApiHash = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SessionData = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Updated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Deleted = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelegramSessions_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TelegramSessions_Users_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TelegramSessions_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TelegramSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelParsingParameters_TelegramSessionId",
                table: "ChannelParsingParameters",
                column: "TelegramSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramSessions_CreatedById",
                table: "TelegramSessions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramSessions_DeletedById",
                table: "TelegramSessions",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramSessions_UpdatedById",
                table: "TelegramSessions",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramSessions_UserId",
                table: "TelegramSessions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChannelParsingParameters_TelegramSessions_TelegramSessionId",
                table: "ChannelParsingParameters",
                column: "TelegramSessionId",
                principalTable: "TelegramSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChannelParsingParameters_TelegramSessions_TelegramSessionId",
                table: "ChannelParsingParameters");

            migrationBuilder.DropTable(
                name: "TelegramSessions");

            migrationBuilder.DropIndex(
                name: "IX_ChannelParsingParameters_TelegramSessionId",
                table: "ChannelParsingParameters");

            migrationBuilder.DropColumn(
                name: "TelegramSessionId",
                table: "ChannelParsingParameters");
        }
    }
}
