using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <summary>
    /// Сбрасывает в Discover признаки доступа, записанные до разделения общего и личного. Старые
    /// CanSendMessages/CanSendMedia смешивали права чата с личными ограничениями и правами админа аккаунта,
    /// а IsBanned ставился и при бане одного аккаунта. Каналы перепроверят импорт, Discover-воркер
    /// и классификатор и запишут каждое значение в своё место. Идёт до MoveDestinationBansToAccountRegistry:
    /// та выставляет общие запреты по журналу репоста, и сброс после неё их бы стёр
    /// </summary>
    public partial class ResetDiscoverAccessFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Сбрасываем и true: разрешение могло прийти из прав админа одного аккаунта. Для импорта
            // null и true равнозначны — канал всё равно проверяется, так что лишних попыток это не даст
            migrationBuilder.Sql("""
                UPDATE "DiscoveredChannels"
                SET "CanSendMessages" = NULL, "CanSendMedia" = NULL
                WHERE "CanSendMessages" IS NOT NULL OR "CanSendMedia" IS NOT NULL;
                """);

            // Несуществующие каналы снова получат IsBanned при перепроверке, а бан одного аккаунта
            // уйдёт в реестр банов сессии
            migrationBuilder.Sql("""
                UPDATE "DiscoveredChannels"
                SET "IsBanned" = false
                WHERE "IsBanned";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Сброс необратим: прежние значения смешивали общее и личное, и восстанавливать их незачем —
            // их заново запишут проверки каналов
        }
    }
}
