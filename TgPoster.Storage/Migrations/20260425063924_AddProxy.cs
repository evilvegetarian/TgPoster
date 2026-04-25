using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Migrations
{
    /// <inheritdoc />
    public partial class AddProxy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProxyId",
                table: "TelegramSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Proxies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Host = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Port = table.Column<int>(type: "integer", nullable: false),
                    Username = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Password = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Secret = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
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
                    table.PrimaryKey("PK_Proxies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Proxies_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Proxies_Users_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Proxies_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Proxies_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramSessions_ProxyId",
                table: "TelegramSessions",
                column: "ProxyId");

            migrationBuilder.CreateIndex(
                name: "IX_Proxies_CreatedById",
                table: "Proxies",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Proxies_DeletedById",
                table: "Proxies",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Proxies_UpdatedById",
                table: "Proxies",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Proxies_UserId",
                table: "Proxies",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TelegramSessions_Proxies_ProxyId",
                table: "TelegramSessions",
                column: "ProxyId",
                principalTable: "Proxies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TelegramSessions_Proxies_ProxyId",
                table: "TelegramSessions");

            migrationBuilder.DropTable(
                name: "Proxies");

            migrationBuilder.DropIndex(
                name: "IX_TelegramSessions_ProxyId",
                table: "TelegramSessions");

            migrationBuilder.DropColumn(
                name: "ProxyId",
                table: "TelegramSessions");
        }
    }
}
