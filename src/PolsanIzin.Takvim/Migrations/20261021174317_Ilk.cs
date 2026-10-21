using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PolsanIzin.Takvim.Migrations
{
    /// <inheritdoc />
    public partial class Ilk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ResmiTatiller",
                columns: table => new
                {
                    Tarih = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Ad = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResmiTatiller", x => x.Tarih);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResmiTatiller");
        }
    }
}
