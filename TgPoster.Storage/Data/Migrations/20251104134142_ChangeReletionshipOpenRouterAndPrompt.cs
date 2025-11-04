using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeReletionshipOpenRouterAndPrompt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PromptSetting_OpenRouterSettings_OpenRouterSettingId",
                table: "PromptSetting");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_PromptSetting_PromptSettingId",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_PromptSetting_OpenRouterSettingId",
                table: "PromptSetting");

            migrationBuilder.DropColumn(
                name: "OpenRouterSettingId",
                table: "PromptSetting");

            migrationBuilder.AlterColumn<Guid>(
                name: "PromptSettingId",
                table: "Schedules",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "OpenRouterSettingId",
                table: "Schedules",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ScheduleId",
                table: "OpenRouterSettings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_OpenRouterSettingId",
                table: "Schedules",
                column: "OpenRouterSettingId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_OpenRouterSettings_OpenRouterSettingId",
                table: "Schedules",
                column: "OpenRouterSettingId",
                principalTable: "OpenRouterSettings",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_PromptSetting_PromptSettingId",
                table: "Schedules",
                column: "PromptSettingId",
                principalTable: "PromptSetting",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_OpenRouterSettings_OpenRouterSettingId",
                table: "Schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_PromptSetting_PromptSettingId",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_OpenRouterSettingId",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "OpenRouterSettingId",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "ScheduleId",
                table: "OpenRouterSettings");

            migrationBuilder.AlterColumn<Guid>(
                name: "PromptSettingId",
                table: "Schedules",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OpenRouterSettingId",
                table: "PromptSetting",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_PromptSetting_OpenRouterSettingId",
                table: "PromptSetting",
                column: "OpenRouterSettingId");

            migrationBuilder.AddForeignKey(
                name: "FK_PromptSetting_OpenRouterSettings_OpenRouterSettingId",
                table: "PromptSetting",
                column: "OpenRouterSettingId",
                principalTable: "OpenRouterSettings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_PromptSetting_PromptSettingId",
                table: "Schedules",
                column: "PromptSettingId",
                principalTable: "PromptSetting",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
