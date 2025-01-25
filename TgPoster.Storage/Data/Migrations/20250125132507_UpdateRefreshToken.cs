using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRefreshToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefreshSession_Users_UserId",
                table: "RefreshSession");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RefreshSession",
                table: "RefreshSession");

            migrationBuilder.RenameTable(
                name: "RefreshSession",
                newName: "RefreshSessions");

            migrationBuilder.RenameIndex(
                name: "IX_RefreshSession_UserId",
                table: "RefreshSessions",
                newName: "IX_RefreshSessions_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RefreshSessions",
                table: "RefreshSessions",
                column: "RefreshToken");

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshSessions_Users_UserId",
                table: "RefreshSessions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefreshSessions_Users_UserId",
                table: "RefreshSessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RefreshSessions",
                table: "RefreshSessions");

            migrationBuilder.RenameTable(
                name: "RefreshSessions",
                newName: "RefreshSession");

            migrationBuilder.RenameIndex(
                name: "IX_RefreshSessions_UserId",
                table: "RefreshSession",
                newName: "IX_RefreshSession_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RefreshSession",
                table: "RefreshSession",
                column: "RefreshToken");

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshSession_Users_UserId",
                table: "RefreshSession",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
