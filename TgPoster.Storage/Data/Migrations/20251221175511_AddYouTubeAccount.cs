using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddYouTubeAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "YouTubeAccountId",
                table: "Schedules",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "YouTubeAccount",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AccessToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ClientId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ClientSecret = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DefaultTitle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DefaultDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DefaultTags = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_YouTubeAccount", x => x.Id);
                    table.ForeignKey(
                        name: "FK_YouTubeAccount_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_YouTubeAccount_Users_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_YouTubeAccount_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_YouTubeAccount_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_YouTubeAccountId",
                table: "Schedules",
                column: "YouTubeAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_YouTubeAccount_CreatedById",
                table: "YouTubeAccount",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_YouTubeAccount_DeletedById",
                table: "YouTubeAccount",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_YouTubeAccount_UpdatedById",
                table: "YouTubeAccount",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_YouTubeAccount_UserId",
                table: "YouTubeAccount",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_YouTubeAccount_YouTubeAccountId",
                table: "Schedules",
                column: "YouTubeAccountId",
                principalTable: "YouTubeAccount",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_YouTubeAccount_YouTubeAccountId",
                table: "Schedules");

            migrationBuilder.DropTable(
                name: "YouTubeAccount");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_YouTubeAccountId",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "YouTubeAccountId",
                table: "Schedules");
        }
    }
}
