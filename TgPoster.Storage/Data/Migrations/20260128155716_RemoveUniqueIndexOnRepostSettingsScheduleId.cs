using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUniqueIndexOnRepostSettingsScheduleId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RepostSettings_ScheduleId",
                table: "RepostSettings");

            migrationBuilder.CreateIndex(
                name: "IX_RepostSettings_ScheduleId",
                table: "RepostSettings",
                column: "ScheduleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RepostSettings_ScheduleId",
                table: "RepostSettings");

            migrationBuilder.CreateIndex(
                name: "IX_RepostSettings_ScheduleId",
                table: "RepostSettings",
                column: "ScheduleId",
                unique: true);
        }
    }
}
