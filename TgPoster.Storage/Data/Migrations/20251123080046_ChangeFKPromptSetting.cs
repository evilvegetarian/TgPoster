using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeFKPromptSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_PromptSettings_PromptSettingId",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_PromptSettingId",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "PromptSettingId",
                table: "Schedules");

            migrationBuilder.AddForeignKey(
                name: "FK_PromptSettings_Schedules_ScheduleId",
                table: "PromptSettings",
                column: "ScheduleId",
                principalTable: "Schedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PromptSettings_Schedules_ScheduleId",
                table: "PromptSettings");

            migrationBuilder.AddColumn<Guid>(
                name: "PromptSettingId",
                table: "Schedules",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_PromptSettingId",
                table: "Schedules",
                column: "PromptSettingId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_PromptSettings_PromptSettingId",
                table: "Schedules",
                column: "PromptSettingId",
                principalTable: "PromptSettings",
                principalColumn: "Id");
        }
    }
}
