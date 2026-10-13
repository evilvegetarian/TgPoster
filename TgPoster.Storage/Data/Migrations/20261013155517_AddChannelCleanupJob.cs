using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChannelCleanupJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChannelCleanupJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TelegramSessionId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_ChannelCleanupJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChannelCleanupJobs_TelegramSessions_TelegramSessionId",
                        column: x => x.TelegramSessionId,
                        principalTable: "TelegramSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChannelCleanupJobs_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChannelCleanupJobs_Users_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChannelCleanupJobs_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ChannelCleanupJobItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelCleanupJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    AccessHash = table.Column<long>(type: "bigint", nullable: false),
                    IsBasicGroup = table.Column<bool>(type: "boolean", nullable: false),
                    ChatType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Username = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Outcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
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
                    table.PrimaryKey("PK_ChannelCleanupJobItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChannelCleanupJobItems_ChannelCleanupJobs_ChannelCleanupJob~",
                        column: x => x.ChannelCleanupJobId,
                        principalTable: "ChannelCleanupJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChannelCleanupJobItems_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChannelCleanupJobItems_Users_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChannelCleanupJobItems_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelCleanupJobItems_ChannelCleanupJobId",
                table: "ChannelCleanupJobItems",
                column: "ChannelCleanupJobId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelCleanupJobItems_CreatedById",
                table: "ChannelCleanupJobItems",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelCleanupJobItems_DeletedById",
                table: "ChannelCleanupJobItems",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelCleanupJobItems_UpdatedById",
                table: "ChannelCleanupJobItems",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelCleanupJobs_CreatedById",
                table: "ChannelCleanupJobs",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelCleanupJobs_DeletedById",
                table: "ChannelCleanupJobs",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelCleanupJobs_Status",
                table: "ChannelCleanupJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelCleanupJobs_TelegramSessionId",
                table: "ChannelCleanupJobs",
                column: "TelegramSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelCleanupJobs_UpdatedById",
                table: "ChannelCleanupJobs",
                column: "UpdatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChannelCleanupJobItems");

            migrationBuilder.DropTable(
                name: "ChannelCleanupJobs");
        }
    }
}
