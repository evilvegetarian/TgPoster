using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Db.Migrations
{
    /// <inheritdoc />
    public partial class AddRepostDestinationChannelInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvatarBase64",
                table: "RepostDestinations",
                type: "character varying(50000)",
                maxLength: 50000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ChatStatus",
                table: "RepostDestinations",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<int>(
                name: "ChatType",
                table: "RepostDestinations",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "DelayMaxSeconds",
                table: "RepostDestinations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DelayMinSeconds",
                table: "RepostDestinations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "InfoUpdatedAt",
                table: "RepostDestinations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxRepostsPerDay",
                table: "RepostDestinations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MemberCount",
                table: "RepostDestinations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RepostCounter",
                table: "RepostDestinations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RepostEveryNth",
                table: "RepostDestinations",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "SkipProbability",
                table: "RepostDestinations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "RepostDestinations",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "RepostDestinations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarBase64",
                table: "RepostDestinations");

            migrationBuilder.DropColumn(
                name: "ChatStatus",
                table: "RepostDestinations");

            migrationBuilder.DropColumn(
                name: "ChatType",
                table: "RepostDestinations");

            migrationBuilder.DropColumn(
                name: "DelayMaxSeconds",
                table: "RepostDestinations");

            migrationBuilder.DropColumn(
                name: "DelayMinSeconds",
                table: "RepostDestinations");

            migrationBuilder.DropColumn(
                name: "InfoUpdatedAt",
                table: "RepostDestinations");

            migrationBuilder.DropColumn(
                name: "MaxRepostsPerDay",
                table: "RepostDestinations");

            migrationBuilder.DropColumn(
                name: "MemberCount",
                table: "RepostDestinations");

            migrationBuilder.DropColumn(
                name: "RepostCounter",
                table: "RepostDestinations");

            migrationBuilder.DropColumn(
                name: "RepostEveryNth",
                table: "RepostDestinations");

            migrationBuilder.DropColumn(
                name: "SkipProbability",
                table: "RepostDestinations");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "RepostDestinations");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "RepostDestinations");
        }
    }
}
