using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPromptSettingAndOpenRouterSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PromptSettingId",
                table: "Schedules",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "OpenRouterSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Model = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Deleted = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenRouterSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpenRouterSettings_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OpenRouterSettings_Users_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OpenRouterSettings_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OpenRouterSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PromptSetting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoPrompt = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    PicturePrompt = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    TextPrompt = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    OpenRouterSettingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Deleted = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromptSetting", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromptSetting_OpenRouterSettings_OpenRouterSettingId",
                        column: x => x.OpenRouterSettingId,
                        principalTable: "OpenRouterSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PromptSetting_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PromptSetting_Users_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PromptSetting_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_PromptSettingId",
                table: "Schedules",
                column: "PromptSettingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpenRouterSettings_CreatedById",
                table: "OpenRouterSettings",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_OpenRouterSettings_DeletedById",
                table: "OpenRouterSettings",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_OpenRouterSettings_UpdatedById",
                table: "OpenRouterSettings",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_OpenRouterSettings_UserId",
                table: "OpenRouterSettings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PromptSetting_CreatedById",
                table: "PromptSetting",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_PromptSetting_DeletedById",
                table: "PromptSetting",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_PromptSetting_OpenRouterSettingId",
                table: "PromptSetting",
                column: "OpenRouterSettingId");

            migrationBuilder.CreateIndex(
                name: "IX_PromptSetting_ScheduleId",
                table: "PromptSetting",
                column: "ScheduleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromptSetting_UpdatedById",
                table: "PromptSetting",
                column: "UpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_PromptSetting_PromptSettingId",
                table: "Schedules",
                column: "PromptSettingId",
                principalTable: "PromptSetting",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_PromptSetting_PromptSettingId",
                table: "Schedules");

            migrationBuilder.DropTable(
                name: "PromptSetting");

            migrationBuilder.DropTable(
                name: "OpenRouterSettings");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_PromptSettingId",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "PromptSettingId",
                table: "Schedules");
        }
    }
}
