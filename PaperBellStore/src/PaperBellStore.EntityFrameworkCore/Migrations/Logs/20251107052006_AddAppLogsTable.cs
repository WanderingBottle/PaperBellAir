using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaperBellStore.Migrations.Logs
{
    /// <inheritdoc />
    public partial class AddAppLogsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Message = table.Column<string>(type: "TEXT", nullable: true),
                    Exception = table.Column<string>(type: "TEXT", nullable: true),
                    Properties = table.Column<string>(type: "JSONB", nullable: true),
                    LogEvent = table.Column<string>(type: "JSONB", nullable: true),
                    MessageHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    FirstOccurrence = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastOccurrence = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    OccurrenceCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    DeduplicationWindowMinutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 5)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppLogs_Level",
                table: "AppLogs",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_AppLogs_MessageHash",
                table: "AppLogs",
                column: "MessageHash");

            migrationBuilder.CreateIndex(
                name: "IX_AppLogs_MessageHash_LastOccurrence",
                table: "AppLogs",
                columns: new[] { "MessageHash", "LastOccurrence" });

            migrationBuilder.CreateIndex(
                name: "IX_AppLogs_Timestamp",
                table: "AppLogs",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppLogs");
        }
    }
}
