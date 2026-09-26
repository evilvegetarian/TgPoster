using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCrossPosting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CrossPostEnabled",
                table: "Messages",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "CrossPostFormat",
                table: "Messages",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SocialAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Platform = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ExternalUserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Secret = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    TokenExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
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
                    table.PrimaryKey("PK_SocialAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SocialAccounts_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SocialAccounts_Users_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SocialAccounts_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SocialAccounts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CrossPostTargets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    SocialAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Format = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LinkTarget = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CustomLink = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CallToAction = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IncludeMedia = table.Column<bool>(type: "boolean", nullable: false),
                    IncludeParsed = table.Column<bool>(type: "boolean", nullable: false),
                    DelayMinutes = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Updated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Deleted = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrossPostTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrossPostTargets_Schedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "Schedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CrossPostTargets_SocialAccounts_SocialAccountId",
                        column: x => x.SocialAccountId,
                        principalTable: "SocialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CrossPostTargets_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CrossPostTargets_Users_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CrossPostTargets_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CrossPosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    CrossPostTargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    SocialAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Platform = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AccountName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExternalPostId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ExternalUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
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
                    table.PrimaryKey("PK_CrossPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrossPosts_CrossPostTargets_CrossPostTargetId",
                        column: x => x.CrossPostTargetId,
                        principalTable: "CrossPostTargets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CrossPosts_Messages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CrossPosts_SocialAccounts_SocialAccountId",
                        column: x => x.SocialAccountId,
                        principalTable: "SocialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CrossPosts_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CrossPosts_Users_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CrossPosts_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CrossPosts_CreatedById",
                table: "CrossPosts",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CrossPosts_CrossPostTargetId",
                table: "CrossPosts",
                column: "CrossPostTargetId");

            migrationBuilder.CreateIndex(
                name: "IX_CrossPosts_DeletedById",
                table: "CrossPosts",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_CrossPosts_MessageId_SocialAccountId",
                table: "CrossPosts",
                columns: new[] { "MessageId", "SocialAccountId" },
                unique: true,
                filter: "\"Deleted\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CrossPosts_SocialAccountId",
                table: "CrossPosts",
                column: "SocialAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CrossPosts_Status_ScheduledAt",
                table: "CrossPosts",
                columns: new[] { "Status", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CrossPosts_UpdatedById",
                table: "CrossPosts",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CrossPostTargets_CreatedById",
                table: "CrossPostTargets",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CrossPostTargets_DeletedById",
                table: "CrossPostTargets",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_CrossPostTargets_ScheduleId_SocialAccountId",
                table: "CrossPostTargets",
                columns: new[] { "ScheduleId", "SocialAccountId" },
                unique: true,
                filter: "\"Deleted\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CrossPostTargets_SocialAccountId",
                table: "CrossPostTargets",
                column: "SocialAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CrossPostTargets_UpdatedById",
                table: "CrossPostTargets",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SocialAccounts_CreatedById",
                table: "SocialAccounts",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SocialAccounts_DeletedById",
                table: "SocialAccounts",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_SocialAccounts_UpdatedById",
                table: "SocialAccounts",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SocialAccounts_UserId_Platform_ExternalUserId",
                table: "SocialAccounts",
                columns: new[] { "UserId", "Platform", "ExternalUserId" },
                unique: true,
                filter: "\"Deleted\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CrossPosts");

            migrationBuilder.DropTable(
                name: "CrossPostTargets");

            migrationBuilder.DropTable(
                name: "SocialAccounts");

            migrationBuilder.DropColumn(
                name: "CrossPostEnabled",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "CrossPostFormat",
                table: "Messages");
        }
    }
}
