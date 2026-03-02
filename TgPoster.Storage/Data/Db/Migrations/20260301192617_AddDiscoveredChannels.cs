using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Db.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscoveredChannels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiscoveredChannels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    AvatarUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ParticipantsCount = table.Column<int>(type: "integer", nullable: true),
                    PeerType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    LastParsedId = table.Column<int>(type: "integer", nullable: true),
                    TgUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Updated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Deleted = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscoveredChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscoveredChannels_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DiscoveredChannels_Users_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DiscoveredChannels_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveredChannels_CreatedById",
                table: "DiscoveredChannels",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveredChannels_DeletedById",
                table: "DiscoveredChannels",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveredChannels_Status",
                table: "DiscoveredChannels",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveredChannels_UpdatedById",
                table: "DiscoveredChannels",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveredChannels_Username",
                table: "DiscoveredChannels",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscoveredChannels");
        }
    }
}
