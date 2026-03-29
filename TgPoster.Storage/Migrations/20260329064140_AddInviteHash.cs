using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Migrations
{
    /// <inheritdoc />
    public partial class AddInviteHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "DiscoveredChannels",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AddColumn<string>(
                name: "InviteHash",
                table: "DiscoveredChannels",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveredChannels_InviteHash",
                table: "DiscoveredChannels",
                column: "InviteHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveredChannels_TelegramId",
                table: "DiscoveredChannels",
                column: "TelegramId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DiscoveredChannels_InviteHash",
                table: "DiscoveredChannels");

            migrationBuilder.DropIndex(
                name: "IX_DiscoveredChannels_TelegramId",
                table: "DiscoveredChannels");

            migrationBuilder.DropColumn(
                name: "InviteHash",
                table: "DiscoveredChannels");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "DiscoveredChannels",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);
        }
    }
}
