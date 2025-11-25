using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeFKOpenRouterSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_OpenRouterSettings_OpenRouterSettingId",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_OpenRouterSettingId",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "OpenRouterSettingId",
                table: "Schedules");

            migrationBuilder.CreateIndex(
                name: "IX_OpenRouterSettings_ScheduleId",
                table: "OpenRouterSettings",
                column: "ScheduleId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OpenRouterSettings_Schedules_ScheduleId",
                table: "OpenRouterSettings",
                column: "ScheduleId",
                principalTable: "Schedules",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OpenRouterSettings_Schedules_ScheduleId",
                table: "OpenRouterSettings");

            migrationBuilder.DropIndex(
                name: "IX_OpenRouterSettings_ScheduleId",
                table: "OpenRouterSettings");

            migrationBuilder.AddColumn<Guid>(
                name: "OpenRouterSettingId",
                table: "Schedules",
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
        }
    }
}
