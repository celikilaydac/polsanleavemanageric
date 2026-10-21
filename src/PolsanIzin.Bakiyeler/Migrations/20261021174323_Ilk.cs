using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PolsanIzin.Bakiyeler.Migrations
{
    /// <inheritdoc />
    public partial class Ilk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IzinHaklari",
                columns: table => new
                {
                    CalisanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Yil = table.Column<int>(type: "INTEGER", nullable: false),
                    Gun = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IzinHaklari", x => new { x.CalisanId, x.Yil });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IzinHaklari");
        }
    }
}
