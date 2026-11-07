using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PolsanIzin.Bakiyeler.Migrations
{
    /// <inheritdoc />
    public partial class BakiyeHareketi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BakiyeHareketleri",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TalepId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CalisanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Yil = table.Column<int>(type: "INTEGER", nullable: false),
                    Gun = table.Column<int>(type: "INTEGER", nullable: false),
                    Tur = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BakiyeHareketleri", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BakiyeHareketleri_CalisanId_Yil",
                table: "BakiyeHareketleri",
                columns: new[] { "CalisanId", "Yil" });

            migrationBuilder.CreateIndex(
                name: "IX_BakiyeHareketleri_TalepId",
                table: "BakiyeHareketleri",
                column: "TalepId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BakiyeHareketleri");
        }
    }
}
