using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeContentType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "MessageFiles");

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "MessageFiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "MessageFiles");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "MessageFiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
