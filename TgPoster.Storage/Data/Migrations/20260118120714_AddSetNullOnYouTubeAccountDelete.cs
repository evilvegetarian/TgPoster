using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSetNullOnYouTubeAccountDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_YouTubeAccounts_YouTubeAccountId",
                table: "Schedules");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_YouTubeAccounts_YouTubeAccountId",
                table: "Schedules",
                column: "YouTubeAccountId",
                principalTable: "YouTubeAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_YouTubeAccounts_YouTubeAccountId",
                table: "Schedules");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_YouTubeAccounts_YouTubeAccountId",
                table: "Schedules",
                column: "YouTubeAccountId",
                principalTable: "YouTubeAccounts",
                principalColumn: "Id");
        }
    }
}
