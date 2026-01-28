using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeNamesTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RepostDestination_RepostSettings_RepostSettingsId",
                table: "RepostDestination");

            migrationBuilder.DropForeignKey(
                name: "FK_RepostDestination_Users_CreatedById",
                table: "RepostDestination");

            migrationBuilder.DropForeignKey(
                name: "FK_RepostDestination_Users_DeletedById",
                table: "RepostDestination");

            migrationBuilder.DropForeignKey(
                name: "FK_RepostDestination_Users_UpdatedById",
                table: "RepostDestination");

            migrationBuilder.DropForeignKey(
                name: "FK_RepostLog_RepostDestination_RepostDestinationId",
                table: "RepostLog");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RepostDestination",
                table: "RepostDestination");

            migrationBuilder.RenameTable(
                name: "RepostDestination",
                newName: "RepostDestinations");

            migrationBuilder.RenameIndex(
                name: "IX_RepostDestination_UpdatedById",
                table: "RepostDestinations",
                newName: "IX_RepostDestinations_UpdatedById");

            migrationBuilder.RenameIndex(
                name: "IX_RepostDestination_RepostSettingsId",
                table: "RepostDestinations",
                newName: "IX_RepostDestinations_RepostSettingsId");

            migrationBuilder.RenameIndex(
                name: "IX_RepostDestination_DeletedById",
                table: "RepostDestinations",
                newName: "IX_RepostDestinations_DeletedById");

            migrationBuilder.RenameIndex(
                name: "IX_RepostDestination_CreatedById",
                table: "RepostDestinations",
                newName: "IX_RepostDestinations_CreatedById");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RepostDestinations",
                table: "RepostDestinations",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RepostDestinations_RepostSettings_RepostSettingsId",
                table: "RepostDestinations",
                column: "RepostSettingsId",
                principalTable: "RepostSettings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RepostDestinations_Users_CreatedById",
                table: "RepostDestinations",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RepostDestinations_Users_DeletedById",
                table: "RepostDestinations",
                column: "DeletedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RepostDestinations_Users_UpdatedById",
                table: "RepostDestinations",
                column: "UpdatedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RepostLog_RepostDestinations_RepostDestinationId",
                table: "RepostLog",
                column: "RepostDestinationId",
                principalTable: "RepostDestinations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RepostDestinations_RepostSettings_RepostSettingsId",
                table: "RepostDestinations");

            migrationBuilder.DropForeignKey(
                name: "FK_RepostDestinations_Users_CreatedById",
                table: "RepostDestinations");

            migrationBuilder.DropForeignKey(
                name: "FK_RepostDestinations_Users_DeletedById",
                table: "RepostDestinations");

            migrationBuilder.DropForeignKey(
                name: "FK_RepostDestinations_Users_UpdatedById",
                table: "RepostDestinations");

            migrationBuilder.DropForeignKey(
                name: "FK_RepostLog_RepostDestinations_RepostDestinationId",
                table: "RepostLog");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RepostDestinations",
                table: "RepostDestinations");

            migrationBuilder.RenameTable(
                name: "RepostDestinations",
                newName: "RepostDestination");

            migrationBuilder.RenameIndex(
                name: "IX_RepostDestinations_UpdatedById",
                table: "RepostDestination",
                newName: "IX_RepostDestination_UpdatedById");

            migrationBuilder.RenameIndex(
                name: "IX_RepostDestinations_RepostSettingsId",
                table: "RepostDestination",
                newName: "IX_RepostDestination_RepostSettingsId");

            migrationBuilder.RenameIndex(
                name: "IX_RepostDestinations_DeletedById",
                table: "RepostDestination",
                newName: "IX_RepostDestination_DeletedById");

            migrationBuilder.RenameIndex(
                name: "IX_RepostDestinations_CreatedById",
                table: "RepostDestination",
                newName: "IX_RepostDestination_CreatedById");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RepostDestination",
                table: "RepostDestination",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RepostDestination_RepostSettings_RepostSettingsId",
                table: "RepostDestination",
                column: "RepostSettingsId",
                principalTable: "RepostSettings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RepostDestination_Users_CreatedById",
                table: "RepostDestination",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RepostDestination_Users_DeletedById",
                table: "RepostDestination",
                column: "DeletedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RepostDestination_Users_UpdatedById",
                table: "RepostDestination",
                column: "UpdatedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RepostLog_RepostDestination_RepostDestinationId",
                table: "RepostLog",
                column: "RepostDestinationId",
                principalTable: "RepostDestination",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
