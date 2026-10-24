using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChannelParsingRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChannelParsingRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelParsingSettingId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    FetchedMessages = table.Column<int>(type: "integer", nullable: false),
                    Posts = table.Column<int>(type: "integer", nullable: false),
                    SkippedByAvoidWords = table.Column<int>(type: "integer", nullable: false),
                    SkippedAsAds = table.Column<int>(type: "integer", nullable: false),
                    SkippedAsDuplicates = table.Column<int>(type: "integer", nullable: false),
                    SkippedEmpty = table.Column<int>(type: "integer", nullable: false),
                    Published = table.Column<int>(type: "integer", nullable: false),
                    LastMessageId = table.Column<int>(type: "integer", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Updated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Deleted = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelParsingRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChannelParsingRuns_ChannelParsingParameters_ChannelParsingS~",
                        column: x => x.ChannelParsingSettingId,
                        principalTable: "ChannelParsingParameters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChannelParsingRuns_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChannelParsingRuns_Users_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChannelParsingRuns_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelParsingRuns_ChannelParsingSettingId_StartedAt",
                table: "ChannelParsingRuns",
                columns: new[] { "ChannelParsingSettingId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelParsingRuns_CreatedById",
                table: "ChannelParsingRuns",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelParsingRuns_DeletedById",
                table: "ChannelParsingRuns",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelParsingRuns_UpdatedById",
                table: "ChannelParsingRuns",
                column: "UpdatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChannelParsingRuns");
        }
    }
}
