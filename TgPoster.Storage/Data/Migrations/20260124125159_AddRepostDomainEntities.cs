using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRepostDomainEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TelegramMessageId",
                table: "Messages",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RepostSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    TelegramSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Updated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Deleted = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepostSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepostSettings_Schedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "Schedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RepostSettings_TelegramSessions_TelegramSessionId",
                        column: x => x.TelegramSessionId,
                        principalTable: "TelegramSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepostSettings_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RepostSettings_Users_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RepostSettings_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RepostDestination",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RepostSettingsId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChatIdentifier = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Updated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Deleted = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepostDestination", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepostDestination_RepostSettings_RepostSettingsId",
                        column: x => x.RepostSettingsId,
                        principalTable: "RepostSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RepostDestination_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RepostDestination_Users_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RepostDestination_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RepostLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    RepostDestinationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TelegramMessageId = table.Column<int>(type: "integer", nullable: true),
                    RepostedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Updated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Deleted = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepostLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepostLog_Messages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepostLog_RepostDestination_RepostDestinationId",
                        column: x => x.RepostDestinationId,
                        principalTable: "RepostDestination",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RepostLog_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RepostLog_Users_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RepostLog_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RepostDestination_CreatedById",
                table: "RepostDestination",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RepostDestination_DeletedById",
                table: "RepostDestination",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RepostDestination_RepostSettingsId",
                table: "RepostDestination",
                column: "RepostSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_RepostDestination_UpdatedById",
                table: "RepostDestination",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RepostLog_CreatedById",
                table: "RepostLog",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RepostLog_DeletedById",
                table: "RepostLog",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RepostLog_MessageId",
                table: "RepostLog",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_RepostLog_RepostDestinationId",
                table: "RepostLog",
                column: "RepostDestinationId");

            migrationBuilder.CreateIndex(
                name: "IX_RepostLog_Status",
                table: "RepostLog",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RepostLog_UpdatedById",
                table: "RepostLog",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RepostSettings_CreatedById",
                table: "RepostSettings",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RepostSettings_DeletedById",
                table: "RepostSettings",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_RepostSettings_ScheduleId",
                table: "RepostSettings",
                column: "ScheduleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepostSettings_TelegramSessionId",
                table: "RepostSettings",
                column: "TelegramSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_RepostSettings_UpdatedById",
                table: "RepostSettings",
                column: "UpdatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RepostLog");

            migrationBuilder.DropTable(
                name: "RepostDestination");

            migrationBuilder.DropTable(
                name: "RepostSettings");

            migrationBuilder.DropColumn(
                name: "TelegramMessageId",
                table: "Messages");
        }
    }
}
