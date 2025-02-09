using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramBot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TelegramBotId",
                table: "Schedules",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "TelegramBots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiTelegram = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Deleted = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramBots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelegramBots_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TelegramBots_Users_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TelegramBots_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TelegramBots_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_TelegramBotId",
                table: "Schedules",
                column: "TelegramBotId");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramBots_CreatedById",
                table: "TelegramBots",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramBots_DeletedById",
                table: "TelegramBots",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramBots_OwnerId",
                table: "TelegramBots",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramBots_UpdatedById",
                table: "TelegramBots",
                column: "UpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_TelegramBots_TelegramBotId",
                table: "Schedules",
                column: "TelegramBotId",
                principalTable: "TelegramBots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_TelegramBots_TelegramBotId",
                table: "Schedules");

            migrationBuilder.DropTable(
                name: "TelegramBots");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_TelegramBotId",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "TelegramBotId",
                table: "Schedules");
        }
    }
}
