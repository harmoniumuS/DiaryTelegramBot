using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaryTelegramBot.Migrations
{
    /// <inheritdoc />
    public partial class CreateUserReminderTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserReminders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ReminderTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReminderMessage = table.Column<string>(type: "TEXT", nullable: false),
                    IsRemind = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserReminders", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserReminders");
        }
    }
}
