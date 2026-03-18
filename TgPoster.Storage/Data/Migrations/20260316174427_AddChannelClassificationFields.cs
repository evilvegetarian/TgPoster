using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChannelClassificationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "DiscoveredChannels",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ClassificationConfidence",
                table: "DiscoveredChannels",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClassifiedAt",
                table: "DiscoveredChannels",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "DiscoveredChannels",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subcategory",
                table: "DiscoveredChannels",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "Tags",
                table: "DiscoveredChannels",
                type: "text[]",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveredChannels_Category",
                table: "DiscoveredChannels",
                column: "Category");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DiscoveredChannels_Category",
                table: "DiscoveredChannels");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "DiscoveredChannels");

            migrationBuilder.DropColumn(
                name: "ClassificationConfidence",
                table: "DiscoveredChannels");

            migrationBuilder.DropColumn(
                name: "ClassifiedAt",
                table: "DiscoveredChannels");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "DiscoveredChannels");

            migrationBuilder.DropColumn(
                name: "Subcategory",
                table: "DiscoveredChannels");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "DiscoveredChannels");
        }
    }
}
