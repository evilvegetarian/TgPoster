using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Db.Migrations
{
    /// <inheritdoc />
    public partial class AddChannelParsingSettingIdAndTotalMessagesCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ChannelParsingSettingId",
                table: "Messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalMessagesCount",
                table: "ChannelParsingParameters",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ChannelParsingSettingId",
                table: "Messages",
                column: "ChannelParsingSettingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_ChannelParsingParameters_ChannelParsingSettingId",
                table: "Messages",
                column: "ChannelParsingSettingId",
                principalTable: "ChannelParsingParameters",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_ChannelParsingParameters_ChannelParsingSettingId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ChannelParsingSettingId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ChannelParsingSettingId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "TotalMessagesCount",
                table: "ChannelParsingParameters");
        }
    }
}
