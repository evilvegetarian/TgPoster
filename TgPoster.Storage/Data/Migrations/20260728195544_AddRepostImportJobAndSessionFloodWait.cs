using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRepostImportJobAndSessionFloodWait : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FloodWaitUntil",
                table: "TelegramSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RepostImportJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RepostSettingsId = table.Column<Guid>(type: "uuid", nullable: false),
                    AutoJoin = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Updated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Deleted = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepostImportJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepostImportJobs_RepostSettings_RepostSettingsId",
                        column: x => x.RepostSettingsId,
                        principalTable: "RepostSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RepostImportJobs_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RepostImportJobs_Users_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RepostImportJobs_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RepostImportJobItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RepostImportJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscoveredChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Outcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RepostDestinationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Updated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Deleted = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepostImportJobItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepostImportJobItems_RepostImportJobs_RepostImportJobId",
                        column: x => x.RepostImportJobId,
                        principalTable: "RepostImportJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RepostImportJobItems_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RepostImportJobItems_Users_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RepostImportJobItems_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RepostImportJobItems_CreatedById",
                table: "RepostImportJobItems",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RepostImportJobItems_DeletedById",
                table: "RepostImportJobItems",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RepostImportJobItems_RepostImportJobId",
                table: "RepostImportJobItems",
                column: "RepostImportJobId");

            migrationBuilder.CreateIndex(
                name: "IX_RepostImportJobItems_UpdatedById",
                table: "RepostImportJobItems",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RepostImportJobs_CreatedById",
                table: "RepostImportJobs",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RepostImportJobs_DeletedById",
                table: "RepostImportJobs",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RepostImportJobs_RepostSettingsId",
                table: "RepostImportJobs",
                column: "RepostSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_RepostImportJobs_Status",
                table: "RepostImportJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RepostImportJobs_UpdatedById",
                table: "RepostImportJobs",
                column: "UpdatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RepostImportJobItems");

            migrationBuilder.DropTable(
                name: "RepostImportJobs");

            migrationBuilder.DropColumn(
                name: "FloodWaitUntil",
                table: "TelegramSessions");
        }
    }
}
