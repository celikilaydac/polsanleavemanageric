using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PolsanIzin.Izinler.Migrations
{
    /// <inheritdoc />
    public partial class IzinTalebi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DenetimKayitlari",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TalepId = table.Column<Guid>(type: "TEXT", nullable: false),
                    YapanCalisanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EskiDurum = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    YeniDurum = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ZamanUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DenetimKayitlari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IzinTalepleri",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CalisanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IzinTuru = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Baslangic = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Bitis = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    IsGunu = table.Column<int>(type: "INTEGER", nullable: false),
                    Aciklama = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Durum = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IzinTalepleri", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DenetimKayitlari_TalepId",
                table: "DenetimKayitlari",
                column: "TalepId");

            migrationBuilder.CreateIndex(
                name: "IX_IzinTalepleri_CalisanId_Durum",
                table: "IzinTalepleri",
                columns: new[] { "CalisanId", "Durum" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DenetimKayitlari");

            migrationBuilder.DropTable(
                name: "IzinTalepleri");
        }
    }
}
