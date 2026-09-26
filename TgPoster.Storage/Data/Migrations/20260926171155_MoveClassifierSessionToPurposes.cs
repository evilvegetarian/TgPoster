using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <inheritdoc />
    public partial class MoveClassifierSessionToPurposes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Сессию, выбранную в настройках, переводим в назначение Classification (= 1 в TelegramSessionPurpose),
            // чтобы после удаления колонки воркер продолжил через неё работать
            migrationBuilder.Sql("""
                                 UPDATE "TelegramSessions"
                                 SET "Purposes" = array_append("Purposes", 1)
                                 WHERE "Id" IN (SELECT "TelegramSessionId"
                                                FROM "ClassifierSettings"
                                                WHERE "TelegramSessionId" IS NOT NULL)
                                   AND NOT (1 = ANY ("Purposes"));
                                 """);

            migrationBuilder.DropForeignKey(
                name: "FK_ClassifierSettings_TelegramSessions_TelegramSessionId",
                table: "ClassifierSettings");

            migrationBuilder.DropIndex(
                name: "IX_ClassifierSettings_TelegramSessionId",
                table: "ClassifierSettings");

            migrationBuilder.DropColumn(
                name: "TelegramSessionId",
                table: "ClassifierSettings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TelegramSessionId",
                table: "ClassifierSettings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassifierSettings_TelegramSessionId",
                table: "ClassifierSettings",
                column: "TelegramSessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassifierSettings_TelegramSessions_TelegramSessionId",
                table: "ClassifierSettings",
                column: "TelegramSessionId",
                principalTable: "TelegramSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
