using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MusicService.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Entries",
                columns: table => new
                {
                    SourceFilePath = table.Column<string>(type: "TEXT", nullable: false),
                    TargetFilePath = table.Column<string>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entries", x => x.SourceFilePath);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Entries_SourceFilePath",
                table: "Entries",
                column: "SourceFilePath");

            migrationBuilder.CreateIndex(
                name: "IX_Entries_TargetFilePath",
                table: "Entries",
                column: "TargetFilePath");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Entries");
        }
    }
}
