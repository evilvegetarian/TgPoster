using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeStructMessageFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "MessageFiles");

            migrationBuilder.DropColumn(
                name: "ThumbnailIds",
                table: "MessageFiles");

            migrationBuilder.AddColumn<string>(
                name: "FileType",
                table: "MessageFiles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "FileThumbnail",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TgFileId = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    MessageFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Updated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Deleted = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileThumbnail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileThumbnail_MessageFiles_MessageFileId",
                        column: x => x.MessageFileId,
                        principalTable: "MessageFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FileThumbnail_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FileThumbnail_Users_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FileThumbnail_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileThumbnail_CreatedById",
                table: "FileThumbnail",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FileThumbnail_DeletedById",
                table: "FileThumbnail",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_FileThumbnail_MessageFileId",
                table: "FileThumbnail",
                column: "MessageFileId");

            migrationBuilder.CreateIndex(
                name: "IX_FileThumbnail_UpdatedById",
                table: "FileThumbnail",
                column: "UpdatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileThumbnail");

            migrationBuilder.DropColumn(
                name: "FileType",
                table: "MessageFiles");

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "MessageFiles",
                type: "character varying(21)",
                maxLength: 21,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailIds",
                table: "MessageFiles",
                type: "json",
                nullable: true);
        }
    }
}
