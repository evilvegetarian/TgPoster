using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageTelegramMessageIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int[]>(
                name: "TelegramMessageIds",
                table: "Messages",
                type: "integer[]",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TelegramTextMessageId",
                table: "Messages",
                type: "integer",
                nullable: true);

            // Id известны только у постов, ушедших одним сообщением: текстовый пост или один файл с подписью.
            // Альбомы и длинный текст при медиа (уходит отдельным сообщением) остаются без id.
            // Воркер сравнивает длину текста в UTF-16, поэтому символы вне BMP (эмодзи) считаются за два
            migrationBuilder.Sql("""
                UPDATE "Messages" m
                SET "TelegramMessageIds" = ARRAY[m."TelegramMessageId"],
                    "TelegramTextMessageId" = m."TelegramMessageId"
                WHERE m."Status" = 2
                  AND m."TelegramMessageId" IS NOT NULL
                  AND m."Deleted" IS NULL
                  AND (
                      SELECT count(*) FROM "MessageFiles" f
                      WHERE f."MessageId" = m."Id"
                        AND f."ParentFileId" IS NULL
                        AND f."Deleted" IS NULL) <= 1
                  AND (
                      NOT EXISTS (
                          SELECT 1 FROM "MessageFiles" f
                          WHERE f."MessageId" = m."Id"
                            AND f."ParentFileId" IS NULL
                            AND f."Deleted" IS NULL)
                      OR m."TextMessage" IS NULL
                      OR length(regexp_replace(m."TextMessage", '[\U00010000-\U0010FFFF]', '__', 'g')) <= 1024);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TelegramMessageIds",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "TelegramTextMessageId",
                table: "Messages");
        }
    }
}
