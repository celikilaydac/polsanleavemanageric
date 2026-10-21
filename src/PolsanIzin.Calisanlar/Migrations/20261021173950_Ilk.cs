using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PolsanIzin.Calisanlar.Migrations
{
    /// <inheritdoc />
    public partial class Ilk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Calisanlar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AdSoyad = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Eposta = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ParolaHash = table.Column<string>(type: "TEXT", nullable: false),
                    YoneticiId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Calisanlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Calisanlar_Calisanlar_YoneticiId",
                        column: x => x.YoneticiId,
                        principalTable: "Calisanlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CalisanRolleri",
                columns: table => new
                {
                    CalisanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Rol = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalisanRolleri", x => new { x.CalisanId, x.Rol });
                    table.ForeignKey(
                        name: "FK_CalisanRolleri_Calisanlar_CalisanId",
                        column: x => x.CalisanId,
                        principalTable: "Calisanlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Calisanlar_Eposta",
                table: "Calisanlar",
                column: "Eposta",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Calisanlar_YoneticiId",
                table: "Calisanlar",
                column: "YoneticiId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalisanRolleri");

            migrationBuilder.DropTable(
                name: "Calisanlar");
        }
    }
}
