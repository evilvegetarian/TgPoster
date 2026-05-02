using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Migrations
{
    /// <inheritdoc />
    public partial class AddParticipantsUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ParticipantsUpdatedAt",
                table: "DiscoveredChannels",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParticipantsUpdatedAt",
                table: "DiscoveredChannels");
        }
    }
}
