using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChannelParsingParameters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChannelParsingParameters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CheckNewPosts = table.Column<bool>(type: "boolean", nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeleteText = table.Column<bool>(type: "boolean", nullable: false),
                    DeleteMedia = table.Column<bool>(type: "boolean", nullable: false),
                    AvoidWords = table.Column<string[]>(type: "text[]", nullable: false),
                    NeedVerifiedPosts = table.Column<bool>(type: "boolean", nullable: false),
                    DateFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    DateTo = table.Column<DateOnly>(type: "date", nullable: true),
                    LastParseId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Deleted = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelParsingParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChannelParsingParameters_Schedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "Schedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChannelParsingParameters_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChannelParsingParameters_Users_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChannelParsingParameters_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelParsingParameters_CreatedById",
                table: "ChannelParsingParameters",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelParsingParameters_DeletedById",
                table: "ChannelParsingParameters",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelParsingParameters_ScheduleId",
                table: "ChannelParsingParameters",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelParsingParameters_UpdatedById",
                table: "ChannelParsingParameters",
                column: "UpdatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChannelParsingParameters");
        }
    }
}
