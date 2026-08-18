using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgPoster.Storage.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRepostLogReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "RepostLog",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "None");

            // Старые записи журнала не хранили причину: успешные оставляем без причины, неудачные помечаем ошибкой пересылки
            migrationBuilder.Sql("UPDATE \"RepostLog\" SET \"Reason\" = 'ForwardFailed' WHERE \"Status\" = 2;");

            migrationBuilder.CreateIndex(
                name: "IX_RepostLog_Created",
                table: "RepostLog",
                column: "Created");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RepostLog_Created",
                table: "RepostLog");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "RepostLog");
        }
    }
}
