using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRepostDiscoverLinkAndPostPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DiscoveredChannelId",
                table: "RepostDestinations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanSendMedia",
                table: "DiscoveredChannels",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanSendMessages",
                table: "DiscoveredChannels",
                type: "boolean",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepostDestinations_DiscoveredChannelId",
                table: "RepostDestinations",
                column: "DiscoveredChannelId");

            migrationBuilder.AddForeignKey(
                name: "FK_RepostDestinations_DiscoveredChannels_DiscoveredChannelId",
                table: "RepostDestinations",
                column: "DiscoveredChannelId",
                principalTable: "DiscoveredChannels",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RepostDestinations_DiscoveredChannels_DiscoveredChannelId",
                table: "RepostDestinations");

            migrationBuilder.DropIndex(
                name: "IX_RepostDestinations_DiscoveredChannelId",
                table: "RepostDestinations");

            migrationBuilder.DropColumn(
                name: "DiscoveredChannelId",
                table: "RepostDestinations");

            migrationBuilder.DropColumn(
                name: "CanSendMedia",
                table: "DiscoveredChannels");

            migrationBuilder.DropColumn(
                name: "CanSendMessages",
                table: "DiscoveredChannels");
        }
    }
}
