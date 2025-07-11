using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIdForRefreshionSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_RefreshSessions",
                table: "RefreshSessions");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "RefreshSessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "Created",
                table: "RefreshSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "RefreshSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Deleted",
                table: "RefreshSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedById",
                table: "RefreshSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Updated",
                table: "RefreshSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "RefreshSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_RefreshSessions",
                table: "RefreshSessions",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshSessions_CreatedById",
                table: "RefreshSessions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshSessions_DeletedById",
                table: "RefreshSessions",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshSessions_RefreshToken",
                table: "RefreshSessions",
                column: "RefreshToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshSessions_UpdatedById",
                table: "RefreshSessions",
                column: "UpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshSessions_Users_CreatedById",
                table: "RefreshSessions",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshSessions_Users_DeletedById",
                table: "RefreshSessions",
                column: "DeletedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshSessions_Users_UpdatedById",
                table: "RefreshSessions",
                column: "UpdatedById",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefreshSessions_Users_CreatedById",
                table: "RefreshSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_RefreshSessions_Users_DeletedById",
                table: "RefreshSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_RefreshSessions_Users_UpdatedById",
                table: "RefreshSessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RefreshSessions",
                table: "RefreshSessions");

            migrationBuilder.DropIndex(
                name: "IX_RefreshSessions_CreatedById",
                table: "RefreshSessions");

            migrationBuilder.DropIndex(
                name: "IX_RefreshSessions_DeletedById",
                table: "RefreshSessions");

            migrationBuilder.DropIndex(
                name: "IX_RefreshSessions_RefreshToken",
                table: "RefreshSessions");

            migrationBuilder.DropIndex(
                name: "IX_RefreshSessions_UpdatedById",
                table: "RefreshSessions");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "RefreshSessions");

            migrationBuilder.DropColumn(
                name: "Created",
                table: "RefreshSessions");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "RefreshSessions");

            migrationBuilder.DropColumn(
                name: "Deleted",
                table: "RefreshSessions");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "RefreshSessions");

            migrationBuilder.DropColumn(
                name: "Updated",
                table: "RefreshSessions");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "RefreshSessions");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RefreshSessions",
                table: "RefreshSessions",
                column: "RefreshToken");
        }
    }
}
