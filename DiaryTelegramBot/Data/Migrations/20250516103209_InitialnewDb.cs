using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaryTelegramBot.Migrations
{
    /// <inheritdoc />
    public partial class InitialnewDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "MessageId",
                table: "UserReminders",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MessageId",
                table: "UserReminders");
        }
    }
}
