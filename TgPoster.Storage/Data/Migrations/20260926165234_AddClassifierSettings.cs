using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClassifierSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastClassificationAttemptAt",
                table: "DiscoveredChannels",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClassifierSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Model = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    BatchSize = table.Column<int>(type: "integer", nullable: false),
                    IntervalMinutes = table.Column<int>(type: "integer", nullable: false),
                    MessageSampleCount = table.Column<int>(type: "integer", nullable: false),
                    PhotoCount = table.Column<int>(type: "integer", nullable: false),
                    ReclassifyAfterDays = table.Column<int>(type: "integer", nullable: true),
                    Categories = table.Column<string[]>(type: "text[]", nullable: false),
                    SystemPrompt = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    TelegramSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Updated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Deleted = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassifierSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassifierSettings_TelegramSessions_TelegramSessionId",
                        column: x => x.TelegramSessionId,
                        principalTable: "TelegramSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ClassifierSettings_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClassifierSettings_Users_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClassifierSettings_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassifierSettings_CreatedById",
                table: "ClassifierSettings",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ClassifierSettings_DeletedById",
                table: "ClassifierSettings",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ClassifierSettings_TelegramSessionId",
                table: "ClassifierSettings",
                column: "TelegramSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassifierSettings_UpdatedById",
                table: "ClassifierSettings",
                column: "UpdatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassifierSettings");

            migrationBuilder.DropColumn(
                name: "LastClassificationAttemptAt",
                table: "DiscoveredChannels");
        }
    }
}
