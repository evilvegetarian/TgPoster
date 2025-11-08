using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangePromptProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PromptSetting_Users_CreatedById",
                table: "PromptSetting");

            migrationBuilder.DropForeignKey(
                name: "FK_PromptSetting_Users_DeletedById",
                table: "PromptSetting");

            migrationBuilder.DropForeignKey(
                name: "FK_PromptSetting_Users_UpdatedById",
                table: "PromptSetting");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_PromptSetting_PromptSettingId",
                table: "Schedules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PromptSetting",
                table: "PromptSetting");

            migrationBuilder.RenameTable(
                name: "PromptSetting",
                newName: "PromptSettings");

            migrationBuilder.RenameIndex(
                name: "IX_PromptSetting_UpdatedById",
                table: "PromptSettings",
                newName: "IX_PromptSettings_UpdatedById");

            migrationBuilder.RenameIndex(
                name: "IX_PromptSetting_ScheduleId",
                table: "PromptSettings",
                newName: "IX_PromptSettings_ScheduleId");

            migrationBuilder.RenameIndex(
                name: "IX_PromptSetting_DeletedById",
                table: "PromptSettings",
                newName: "IX_PromptSettings_DeletedById");

            migrationBuilder.RenameIndex(
                name: "IX_PromptSetting_CreatedById",
                table: "PromptSettings",
                newName: "IX_PromptSettings_CreatedById");

            migrationBuilder.AlterColumn<string>(
                name: "VideoPrompt",
                table: "PromptSettings",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(5000)",
                oldMaxLength: 5000);

            migrationBuilder.AlterColumn<string>(
                name: "TextPrompt",
                table: "PromptSettings",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(5000)",
                oldMaxLength: 5000);

            migrationBuilder.AlterColumn<string>(
                name: "PicturePrompt",
                table: "PromptSettings",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(5000)",
                oldMaxLength: 5000);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PromptSettings",
                table: "PromptSettings",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PromptSettings_Users_CreatedById",
                table: "PromptSettings",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PromptSettings_Users_DeletedById",
                table: "PromptSettings",
                column: "DeletedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PromptSettings_Users_UpdatedById",
                table: "PromptSettings",
                column: "UpdatedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_PromptSettings_PromptSettingId",
                table: "Schedules",
                column: "PromptSettingId",
                principalTable: "PromptSettings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PromptSettings_Users_CreatedById",
                table: "PromptSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_PromptSettings_Users_DeletedById",
                table: "PromptSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_PromptSettings_Users_UpdatedById",
                table: "PromptSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_PromptSettings_PromptSettingId",
                table: "Schedules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PromptSettings",
                table: "PromptSettings");

            migrationBuilder.RenameTable(
                name: "PromptSettings",
                newName: "PromptSetting");

            migrationBuilder.RenameIndex(
                name: "IX_PromptSettings_UpdatedById",
                table: "PromptSetting",
                newName: "IX_PromptSetting_UpdatedById");

            migrationBuilder.RenameIndex(
                name: "IX_PromptSettings_ScheduleId",
                table: "PromptSetting",
                newName: "IX_PromptSetting_ScheduleId");

            migrationBuilder.RenameIndex(
                name: "IX_PromptSettings_DeletedById",
                table: "PromptSetting",
                newName: "IX_PromptSetting_DeletedById");

            migrationBuilder.RenameIndex(
                name: "IX_PromptSettings_CreatedById",
                table: "PromptSetting",
                newName: "IX_PromptSetting_CreatedById");

            migrationBuilder.AlterColumn<string>(
                name: "VideoPrompt",
                table: "PromptSetting",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(5000)",
                oldMaxLength: 5000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TextPrompt",
                table: "PromptSetting",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(5000)",
                oldMaxLength: 5000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PicturePrompt",
                table: "PromptSetting",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(5000)",
                oldMaxLength: 5000,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PromptSetting",
                table: "PromptSetting",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PromptSetting_Users_CreatedById",
                table: "PromptSetting",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PromptSetting_Users_DeletedById",
                table: "PromptSetting",
                column: "DeletedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PromptSetting_Users_UpdatedById",
                table: "PromptSetting",
                column: "UpdatedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_PromptSetting_PromptSettingId",
                table: "Schedules",
                column: "PromptSettingId",
                principalTable: "PromptSetting",
                principalColumn: "Id");
        }
    }
}
