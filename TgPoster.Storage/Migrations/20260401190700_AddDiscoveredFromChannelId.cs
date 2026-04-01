using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscoveredFromChannelId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DiscoveredFromChannelId",
                table: "DiscoveredChannels",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveredChannels_DiscoveredFromChannelId",
                table: "DiscoveredChannels",
                column: "DiscoveredFromChannelId");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscoveredChannels_DiscoveredChannels_DiscoveredFromChannel~",
                table: "DiscoveredChannels",
                column: "DiscoveredFromChannelId",
                principalTable: "DiscoveredChannels",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiscoveredChannels_DiscoveredChannels_DiscoveredFromChannel~",
                table: "DiscoveredChannels");

            migrationBuilder.DropIndex(
                name: "IX_DiscoveredChannels_DiscoveredFromChannelId",
                table: "DiscoveredChannels");

            migrationBuilder.DropColumn(
                name: "DiscoveredFromChannelId",
                table: "DiscoveredChannels");
        }
    }
}
