using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultRepostSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultDelayMaxSeconds",
                table: "RepostSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DefaultDelayMinSeconds",
                table: "RepostSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DefaultMaxRepostsPerDay",
                table: "RepostSettings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultRepostEveryNth",
                table: "RepostSettings",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "DefaultSkipProbability",
                table: "RepostSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultDelayMaxSeconds",
                table: "RepostSettings");

            migrationBuilder.DropColumn(
                name: "DefaultDelayMinSeconds",
                table: "RepostSettings");

            migrationBuilder.DropColumn(
                name: "DefaultMaxRepostsPerDay",
                table: "RepostSettings");

            migrationBuilder.DropColumn(
                name: "DefaultRepostEveryNth",
                table: "RepostSettings");

            migrationBuilder.DropColumn(
                name: "DefaultSkipProbability",
                table: "RepostSettings");
        }
    }
}
