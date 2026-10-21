using System.Net;

namespace PolsanIzin.Web.Tests;

/// <summary>Şema kurulumu (AC-4) ve ortama göre demo veri (AC-20, AC-23, AC-24, AC-25).</summary>
[Trait("Kategori", "Entegrasyon")]
public sealed class SemaVeDemoVeriTests
{
    private static readonly string[] _modulTablolari = ["Calisanlar", "CalisanRolleri", "ResmiTatiller", "IzinHaklari"];

    private static readonly string[] _gecmisTablolari =
        ["__EFMigrationsHistory_Calisanlar", "__EFMigrationsHistory_Takvim", "__EFMigrationsHistory_Bakiyeler"];

    [Fact]
    public async Task Acilis_BosVeritabani_TumSemalarKurulur()
    {
        using var factory = new PolsanIzinFactory { TestVerisiYukle = false };
        await factory.Istemci().AlAsync("/Giris");

        foreach (var tablo in _modulTablolari.Concat(_gecmisTablolari))
        {
            Assert.Equal(1, factory.SorguSayisi($"SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = '{tablo}'"));
        }
    }

    [Fact]
    public async Task Acilis_IkinciAcilis_HataVermez()
    {
        using var ilk = new PolsanIzinFactory { TestVerisiYukle = false };
        await ilk.Istemci().AlAsync("/Giris");

        using var ikinci = new PolsanIzinFactory(ilk.BaglantiDizesi) { TestVerisiYukle = false };
        var yanit = await ikinci.Istemci().AlAsync("/Giris");

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
    }

    [Fact]
    public async Task Development_DemoVeriYuklenir_HerDemoCalisanGirisYapabilir()
    {
        using var factory = new PolsanIzinFactory { Ortam = "Development", TestVerisiYukle = false };
        var ilkIstemci = factory.Istemci();
        await ilkIstemci.AlAsync("/Giris");

        Assert.Equal(8, factory.Say("Calisanlar"));
        Assert.Equal(27, factory.Say("ResmiTatiller"));
        Assert.Equal(8, factory.Say("IzinHaklari"));
        foreach (var eposta in DemoKullanicilar.Epostalar)
        {
            var yanit = await factory.Istemci().GirisYapAsync(eposta, DemoKullanicilar.Parola);
            Assert.True(yanit.StatusCode == HttpStatusCode.Redirect && yanit.OturumCereziYazar(), eposta);
        }
    }

    [Fact]
    public async Task Development_IkiAcilis_SayilarDegismez()
    {
        using var ilk = new PolsanIzinFactory { Ortam = "Development", TestVerisiYukle = false };
        await ilk.Istemci().AlAsync("/Giris");
        var once = _modulTablolari.Select(ilk.Say).ToArray();

        using var ikinci = new PolsanIzinFactory(ilk.BaglantiDizesi) { Ortam = "Development", TestVerisiYukle = false };
        await ikinci.Istemci().AlAsync("/Giris");

        Assert.Equal(once, _modulTablolari.Select(ikinci.Say).ToArray());
    }

    [Fact]
    public async Task Development_HerHakBirDemoCalisanaBagli()
    {
        // ADR 0003: demo Guid'leri Calisanlar ve Bakiyeler'de tekrarlanır; tutarlılık burada korunur.
        using var factory = new PolsanIzinFactory { Ortam = "Development", TestVerisiYukle = false };
        await factory.Istemci().AlAsync("/Giris");

        Assert.Equal(0, factory.SorguSayisi(
            "SELECT COUNT(*) FROM IzinHaklari h LEFT JOIN Calisanlar c ON c.Id = h.CalisanId WHERE c.Id IS NULL"));
        Assert.Equal(0, factory.SorguSayisi(
            "SELECT COUNT(*) FROM Calisanlar c LEFT JOIN IzinHaklari h ON h.CalisanId = c.Id AND h.Yil = 2026 WHERE h.CalisanId IS NULL"));
        Assert.Equal(8, factory.SorguSayisi("SELECT COUNT(*) FROM IzinHaklari WHERE Yil = 2026 AND Gun = 14"));
        Assert.Equal(0, factory.SorguSayisi("SELECT COUNT(*) FROM IzinHaklari WHERE Yil = 2027"));
    }

    [Fact]
    public async Task Production_DemoVeriYok_DemoEpostaIleGirisYapilamaz()
    {
        using var factory = new PolsanIzinFactory { Ortam = "Production", TestVerisiYukle = false };
        var istemci = factory.Istemci();

        var yanit = await istemci.GirisYapAsync("ik1@polsan.example", DemoKullanicilar.Parola);

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        Assert.False(yanit.OturumCereziYazar());
        foreach (var tablo in _modulTablolari)
        {
            Assert.Equal(0, factory.Say(tablo));
        }
    }
}
