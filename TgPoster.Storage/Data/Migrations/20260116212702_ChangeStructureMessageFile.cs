using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeStructureMessageFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileThumbnail");

            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "MessageFiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentFileId",
                table: "MessageFiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessageFiles_ParentFileId",
                table: "MessageFiles",
                column: "ParentFileId");

            migrationBuilder.AddForeignKey(
                name: "FK_MessageFiles_MessageFiles_ParentFileId",
                table: "MessageFiles",
                column: "ParentFileId",
                principalTable: "MessageFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MessageFiles_MessageFiles_ParentFileId",
                table: "MessageFiles");

            migrationBuilder.DropIndex(
                name: "IX_MessageFiles_ParentFileId",
                table: "MessageFiles");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "MessageFiles");

            migrationBuilder.DropColumn(
                name: "ParentFileId",
                table: "MessageFiles");

            migrationBuilder.CreateTable(
                name: "FileThumbnail",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uuid", nullable: true),
                    MessageFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Deleted = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TgFileId = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Updated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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
    }
}
