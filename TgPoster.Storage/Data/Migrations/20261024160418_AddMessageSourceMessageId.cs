using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageSourceMessageId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Messages_ChannelParsingSettingId",
                table: "Messages");

            migrationBuilder.AddColumn<int>(
                name: "SourceMessageId",
                table: "Messages",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ChannelParsingSettingId_SourceMessageId",
                table: "Messages",
                columns: new[] { "ChannelParsingSettingId", "SourceMessageId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Messages_ChannelParsingSettingId_SourceMessageId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "SourceMessageId",
                table: "Messages");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ChannelParsingSettingId",
                table: "Messages",
                column: "ChannelParsingSettingId");
        }
    }
}
