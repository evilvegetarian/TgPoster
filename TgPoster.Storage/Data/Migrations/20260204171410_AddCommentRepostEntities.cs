using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCommentRepostEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommentRepostSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WatchedChannel = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    WatchedChannelId = table.Column<long>(type: "bigint", nullable: false),
                    WatchedChannelAccessHash = table.Column<long>(type: "bigint", nullable: true),
                    DiscussionGroupId = table.Column<long>(type: "bigint", nullable: false),
                    DiscussionGroupAccessHash = table.Column<long>(type: "bigint", nullable: true),
                    TelegramSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    LastProcessedPostId = table.Column<int>(type: "integer", nullable: true),
                    LastCheckDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Updated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Deleted = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentRepostSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommentRepostSettings_Schedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "Schedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommentRepostSettings_TelegramSessions_TelegramSessionId",
                        column: x => x.TelegramSessionId,
                        principalTable: "TelegramSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommentRepostSettings_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CommentRepostSettings_Users_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CommentRepostSettings_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CommentRepostLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommentRepostSettingsId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalPostId = table.Column<int>(type: "integer", nullable: false),
                    ForwardedMessageId = table.Column<int>(type: "integer", nullable: true),
                    CommentMessageId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Updated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Deleted = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentRepostLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommentRepostLogs_CommentRepostSettings_CommentRepostSettin~",
                        column: x => x.CommentRepostSettingsId,
                        principalTable: "CommentRepostSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommentRepostLogs_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CommentRepostLogs_Users_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CommentRepostLogs_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommentRepostLogs_CommentRepostSettingsId",
                table: "CommentRepostLogs",
                column: "CommentRepostSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentRepostLogs_CreatedById",
                table: "CommentRepostLogs",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CommentRepostLogs_DeletedById",
                table: "CommentRepostLogs",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_CommentRepostLogs_Status",
                table: "CommentRepostLogs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CommentRepostLogs_UpdatedById",
                table: "CommentRepostLogs",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CommentRepostSettings_CreatedById",
                table: "CommentRepostSettings",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CommentRepostSettings_DeletedById",
                table: "CommentRepostSettings",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_CommentRepostSettings_ScheduleId",
                table: "CommentRepostSettings",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentRepostSettings_TelegramSessionId",
                table: "CommentRepostSettings",
                column: "TelegramSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentRepostSettings_UpdatedById",
                table: "CommentRepostSettings",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CommentRepostSettings_WatchedChannelId_ScheduleId",
                table: "CommentRepostSettings",
                columns: new[] { "WatchedChannelId", "ScheduleId" },
                unique: true,
                filter: "\"Deleted\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommentRepostLogs");

            migrationBuilder.DropTable(
                name: "CommentRepostSettings");
        }
    }
}
