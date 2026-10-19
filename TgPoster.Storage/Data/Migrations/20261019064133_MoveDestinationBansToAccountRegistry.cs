using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <summary>
    /// Разносит старый статус направления «забанен» (ChatStatus = 1) по местам хранения: бан аккаунта —
    /// в реестр банов сессии, общий запрет чата — во флаги Discover. Причина берётся из последней записи
    /// журнала репоста с причиной Banned. После миграции в направлении остаётся только членство в чате
    /// </summary>
    public partial class MoveDestinationBansToAccountRegistry : Migration
    {
        /// <summary>
        /// Направления со старым статусом Banned вместе с последней ошибкой бана из журнала
        /// </summary>
        private const string BannedDestinations = """
            SELECT d."Id", d."ChatId", d."DiscoveredChannelId", s."TelegramSessionId",
                   coalesce(l."Error", '') AS "Error"
            FROM "RepostDestinations" d
            JOIN "RepostSettings" s ON s."Id" = d."RepostSettingsId"
            LEFT JOIN LATERAL (
                SELECT r."Error"
                FROM "RepostLog" r
                WHERE r."RepostDestinationId" = d."Id" AND r."Reason" = 'Banned'
                ORDER BY r."Created" DESC NULLS LAST
                LIMIT 1) l ON true
            WHERE d."Deleted" IS NULL AND d."ChatStatus" = 1
            """;

        // Те же правила, что у Telegram-слоя: писать нельзя никому
        private const string WriteForbidden = """
            (b."Error" IN ('CHAT_WRITE_FORBIDDEN', 'CHAT_SEND_PLAIN_FORBIDDEN')
             OR b."Error" LIKE 'ALLOW_PAYMENT_REQUIRED%')
            """;

        // Медиа нельзя никому
        private const string MediaForbidden = """
            (b."Error" ~ '^CHAT_SEND_.*_FORBIDDEN$' AND b."Error" <> 'CHAT_SEND_PLAIN_FORBIDDEN')
            """;

        // Направление в Discover: по связке, а для старых направлений без связки — по id чата
        private const string DiscoverMatch = """
            (c."Id" = b."DiscoveredChannelId" OR (b."DiscoveredChannelId" IS NULL AND c."TelegramId" = b."ChatId"))
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Всё остальное, включая статус без записи в журнале (его ставило «Обновить информацию»
            // при ChannelForbidden), — бан именно этого аккаунта
            migrationBuilder.Sql($"""
                WITH b AS ({BannedDestinations})
                INSERT INTO "TelegramSessionChatBans" ("Id", "TelegramSessionId", "ChatId", "Error", "DetectedAt", "Created")
                SELECT DISTINCT ON (b."TelegramSessionId", b."ChatId")
                       gen_random_uuid(), b."TelegramSessionId", b."ChatId",
                       left(CASE WHEN b."Error" = '' THEN 'Перенесено из статуса направления' ELSE b."Error" END, 500),
                       now(), now()
                FROM b
                WHERE NOT ({WriteForbidden} OR {MediaForbidden})
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.Sql($"""
                WITH b AS ({BannedDestinations})
                UPDATE "DiscoveredChannels" c SET "CanSendMessages" = false
                FROM b
                WHERE {WriteForbidden} AND {DiscoverMatch};
                """);

            migrationBuilder.Sql($"""
                WITH b AS ({BannedDestinations})
                UPDATE "DiscoveredChannels" c SET "CanSendMedia" = false
                FROM b
                WHERE {MediaForbidden} AND {DiscoverMatch};
                """);

            // При общем запрете аккаунт в чате состоит, при бане членство неизвестно: его проверит включение.
            // Выполняется последним — до этого места строки ещё отбираются по ChatStatus = 1
            migrationBuilder.Sql($"""
                WITH b AS ({BannedDestinations})
                UPDATE "RepostDestinations" d
                SET "ChatStatus" = CASE WHEN {WriteForbidden} OR {MediaForbidden} THEN 0 ELSE 3 END
                FROM b
                WHERE d."Id" = b."Id";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Бан аккаунта возвращается в статус направления. Общие запреты, перенесённые в Discover,
            // назад не отличить от записанных там раньше — они остаются в Discover
            migrationBuilder.Sql("""
                UPDATE "RepostDestinations" d SET "ChatStatus" = 1
                FROM "RepostSettings" s
                WHERE s."Id" = d."RepostSettingsId"
                  AND d."Deleted" IS NULL
                  AND EXISTS (
                      SELECT 1 FROM "TelegramSessionChatBans" t
                      WHERE t."Deleted" IS NULL
                        AND t."TelegramSessionId" = s."TelegramSessionId"
                        AND t."ChatId" = d."ChatId");
                """);
        }
    }
}
