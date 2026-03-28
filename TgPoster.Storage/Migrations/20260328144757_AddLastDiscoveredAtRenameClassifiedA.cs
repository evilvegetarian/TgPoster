using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Migrations
{
    /// <inheritdoc />
    public partial class AddLastDiscoveredAtRenameClassifiedA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ClassifiedAt",
                table: "DiscoveredChannels",
                newName: "LastDiscoveredAt");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastClassifiedAt",
                table: "DiscoveredChannels",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastClassifiedAt",
                table: "DiscoveredChannels");

            migrationBuilder.RenameColumn(
                name: "LastDiscoveredAt",
                table: "DiscoveredChannels",
                newName: "ClassifiedAt");
        }
    }
}
