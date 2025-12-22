using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFkForYouTubeAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_YouTubeAccount_YouTubeAccountId",
                table: "Schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_YouTubeAccount_Users_CreatedById",
                table: "YouTubeAccount");

            migrationBuilder.DropForeignKey(
                name: "FK_YouTubeAccount_Users_DeletedById",
                table: "YouTubeAccount");

            migrationBuilder.DropForeignKey(
                name: "FK_YouTubeAccount_Users_UpdatedById",
                table: "YouTubeAccount");

            migrationBuilder.DropForeignKey(
                name: "FK_YouTubeAccount_Users_UserId",
                table: "YouTubeAccount");

            migrationBuilder.DropPrimaryKey(
                name: "PK_YouTubeAccount",
                table: "YouTubeAccount");

            migrationBuilder.RenameTable(
                name: "YouTubeAccount",
                newName: "YouTubeAccounts");

            migrationBuilder.RenameIndex(
                name: "IX_YouTubeAccount_UserId",
                table: "YouTubeAccounts",
                newName: "IX_YouTubeAccounts_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_YouTubeAccount_UpdatedById",
                table: "YouTubeAccounts",
                newName: "IX_YouTubeAccounts_UpdatedById");

            migrationBuilder.RenameIndex(
                name: "IX_YouTubeAccount_DeletedById",
                table: "YouTubeAccounts",
                newName: "IX_YouTubeAccounts_DeletedById");

            migrationBuilder.RenameIndex(
                name: "IX_YouTubeAccount_CreatedById",
                table: "YouTubeAccounts",
                newName: "IX_YouTubeAccounts_CreatedById");

            migrationBuilder.AddPrimaryKey(
                name: "PK_YouTubeAccounts",
                table: "YouTubeAccounts",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_YouTubeAccounts_YouTubeAccountId",
                table: "Schedules",
                column: "YouTubeAccountId",
                principalTable: "YouTubeAccounts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_YouTubeAccounts_Users_CreatedById",
                table: "YouTubeAccounts",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_YouTubeAccounts_Users_DeletedById",
                table: "YouTubeAccounts",
                column: "DeletedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_YouTubeAccounts_Users_UpdatedById",
                table: "YouTubeAccounts",
                column: "UpdatedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_YouTubeAccounts_Users_UserId",
                table: "YouTubeAccounts",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_YouTubeAccounts_YouTubeAccountId",
                table: "Schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_YouTubeAccounts_Users_CreatedById",
                table: "YouTubeAccounts");

            migrationBuilder.DropForeignKey(
                name: "FK_YouTubeAccounts_Users_DeletedById",
                table: "YouTubeAccounts");

            migrationBuilder.DropForeignKey(
                name: "FK_YouTubeAccounts_Users_UpdatedById",
                table: "YouTubeAccounts");

            migrationBuilder.DropForeignKey(
                name: "FK_YouTubeAccounts_Users_UserId",
                table: "YouTubeAccounts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_YouTubeAccounts",
                table: "YouTubeAccounts");

            migrationBuilder.RenameTable(
                name: "YouTubeAccounts",
                newName: "YouTubeAccount");

            migrationBuilder.RenameIndex(
                name: "IX_YouTubeAccounts_UserId",
                table: "YouTubeAccount",
                newName: "IX_YouTubeAccount_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_YouTubeAccounts_UpdatedById",
                table: "YouTubeAccount",
                newName: "IX_YouTubeAccount_UpdatedById");

            migrationBuilder.RenameIndex(
                name: "IX_YouTubeAccounts_DeletedById",
                table: "YouTubeAccount",
                newName: "IX_YouTubeAccount_DeletedById");

            migrationBuilder.RenameIndex(
                name: "IX_YouTubeAccounts_CreatedById",
                table: "YouTubeAccount",
                newName: "IX_YouTubeAccount_CreatedById");

            migrationBuilder.AddPrimaryKey(
                name: "PK_YouTubeAccount",
                table: "YouTubeAccount",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_YouTubeAccount_YouTubeAccountId",
                table: "Schedules",
                column: "YouTubeAccountId",
                principalTable: "YouTubeAccount",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_YouTubeAccount_Users_CreatedById",
                table: "YouTubeAccount",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_YouTubeAccount_Users_DeletedById",
                table: "YouTubeAccount",
                column: "DeletedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_YouTubeAccount_Users_UpdatedById",
                table: "YouTubeAccount",
                column: "UpdatedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_YouTubeAccount_Users_UserId",
                table: "YouTubeAccount",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
