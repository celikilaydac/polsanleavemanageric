using System.Net;
using AngleSharp.Html.Dom;
using PolsanIzin.Izinler;

namespace PolsanIzin.Web.Tests;

/// <summary>Geçmiş sekmesi (spec 0004). Factory saati 2026-10-21 09:00 UTC (= 12:00 Istanbul).</summary>
[Trait("Kategori", "Entegrasyon")]
public sealed class GecmisSayfasiTests : IDisposable
{
    private const string _adres = "/Izinler/Gecmis";

    private readonly PolsanIzinFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private static DateOnly T(int ay, int gun) => new(2026, ay, gun);

    private async Task<IHtmlDocument> SayfaAsync(string eposta = "ik1@polsan.example", string adres = _adres)
    {
        var istemci = _factory.Istemci();
        await istemci.GirisYapmisAsync(eposta);
        var yanit = await istemci.AlAsync(adres);
        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        return await yanit.HtmlAsync();
    }

    private static List<string[]> Satirlar(IHtmlDocument belge) =>
    [
        .. belge.QuerySelectorAll("#gecmis tbody tr")
            .Select(tr => tr.QuerySelectorAll("td").Select(td => td.TextContent.Trim()).ToArray()),
    ];

    private (long, long, long) Sayilar() => (
        _factory.SorguSayisi("SELECT COUNT(*) FROM \"IzinTalepleri\""),
        _factory.SorguSayisi("SELECT COUNT(*) FROM \"DenetimKayitlari\""),
        _factory.SorguSayisi("SELECT COUNT(*) FROM \"BakiyeHareketleri\""));

    [Fact]
    public async Task Get_Olusturma_IstanbulZamaniVeSiz()
    {
        var ik1 = await _factory.CalisanIdAsync("ik1@polsan.example");
        await _factory.TalepAsync(ik1, IzinTuru.Yillik, T(11, 2), T(11, 6));

        var belge = await SayfaAsync();

        Assert.Equal(
            ["21.10.2026 12:00", "Yıllık 02.11.2026 – 06.11.2026", "Oluşturuldu → Beklemede", "Siz"],
            Assert.Single(Satirlar(belge)));
    }

    [Fact]
    public async Task Get_UtcAksam_IstanbulErtesiGun()
    {
        _factory.Zaman.SetUtcNow(new DateTimeOffset(2026, 10, 21, 21, 30, 0, TimeSpan.Zero));
        var ik1 = await _factory.CalisanIdAsync("ik1@polsan.example");
        await _factory.TalepAsync(ik1, IzinTuru.Mazeret, T(11, 2), T(11, 2));

        var belge = await SayfaAsync();

        Assert.Equal("22.10.2026 00:30", Assert.Single(Satirlar(belge))[0]);
    }

    [Fact]
    public async Task Get_IkiKayit_YenidenEskiye()
    {
        var ik1 = await _factory.CalisanIdAsync("ik1@polsan.example");
        await _factory.TalepAsync(ik1, IzinTuru.Mazeret, T(11, 2), T(11, 2));
        _factory.Zaman.Advance(TimeSpan.FromHours(1));
        await _factory.TalepAsync(ik1, IzinTuru.Ucretsiz, T(11, 3), T(11, 3));

        var belge = await SayfaAsync();

        Assert.Equal(
            [("21.10.2026 13:00", "Ücretsiz 03.11.2026 – 03.11.2026"), ("21.10.2026 12:00", "Mazeret 02.11.2026 – 02.11.2026")],
            Satirlar(belge).Select(s => (s[0], s[1])));
    }

    [Fact]
    public async Task Get_BaskasininDegisikligi_DurumlarVeAdSoyad()
    {
        var ik1 = await _factory.CalisanIdAsync("ik1@polsan.example");
        var yonetici = await _factory.CalisanIdAsync("yonetici.a@polsan.example");
        await _factory.TalepAsync(ik1, IzinTuru.Mazeret, T(11, 2), T(11, 2));
        // Onay henüz public API'de yok; yöneticinin onay kaydı doğrudan kurulur (plan 0004 Risk 2).
        var zaman = new DateTimeOffset(2026, 10, 21, 11, 0, 0, TimeSpan.Zero).UtcTicks;
        _factory.Calistir($"""
            INSERT INTO "DenetimKayitlari" ("Id", "TalepId", "YapanCalisanId", "EskiDurum", "YeniDurum", "ZamanUtc")
            SELECT upper('{Guid.CreateVersion7()}'), "Id", upper('{yonetici}'), 'Beklemede', 'Onaylandi', {zaman}
            FROM "IzinTalepleri"
            """);

        var belge = await SayfaAsync();

        var satirlar = Satirlar(belge);
        Assert.Equal(2, satirlar.Count);
        Assert.Equal(
            ["21.10.2026 14:00", "Mazeret 02.11.2026 – 02.11.2026", "Beklemede → Onaylandı", "Zeynep Yıldız"],
            satirlar[0]);
        Assert.Equal("Siz", satirlar[1][3]);
    }

    [Fact]
    public async Task Get_BaskaCalisan_KayitlariGorunmez()
    {
        var ik1 = await _factory.CalisanIdAsync("ik1@polsan.example");
        var a1 = await _factory.CalisanIdAsync("calisan.a1@polsan.example");
        await _factory.TalepAsync(ik1, IzinTuru.Mazeret, T(11, 2), T(11, 2));
        await _factory.TalepAsync(a1, IzinTuru.Ucretsiz, T(11, 9), T(11, 9));

        var belge = await SayfaAsync(adres: $"{_adres}?calisanId={a1}&CalisanId={a1}");

        Assert.Equal("Mazeret 02.11.2026 – 02.11.2026", Assert.Single(Satirlar(belge))[1]);
    }

    [Fact]
    public async Task Get_KayitYok_BosDurum()
    {
        var belge = await SayfaAsync();

        Assert.Equal("Henüz kayıt yok.", belge.GetElementById("bos")?.TextContent.Trim());
        Assert.Null(belge.QuerySelector("table"));
    }

    [Fact]
    public async Task Get_OturumYok_GiriseYonlendirir()
    {
        var yanit = await _factory.Istemci().AlAsync(_adres);

        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        Assert.StartsWith("https://localhost/Giris", yanit.Konum(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_Erisilebilirlik_CaptionVeBasliklar()
    {
        var ik1 = await _factory.CalisanIdAsync("ik1@polsan.example");
        await _factory.TalepAsync(ik1, IzinTuru.Mazeret, T(11, 2), T(11, 2));

        var belge = await SayfaAsync();

        Assert.False(string.IsNullOrWhiteSpace(belge.QuerySelector("#gecmis caption")?.TextContent));
        Assert.Equal(
            ["Zaman", "Talep", "Değişiklik", "Yapan"],
            belge.QuerySelectorAll("#gecmis th[scope=col]").Select(th => th.TextContent.Trim()));
        Assert.Equal("Geçmiş", belge.QuerySelector("nav a[aria-current=page]")?.TextContent.Trim());
        Assert.Empty(belge.QuerySelectorAll("main form"));
        Assert.Empty(belge.QuerySelectorAll("script"));
    }

    [Fact]
    public async Task Get_SayilarDegismez()
    {
        var ik1 = await _factory.CalisanIdAsync("ik1@polsan.example");
        await _factory.TalepAsync(ik1, IzinTuru.Yillik, T(11, 2), T(11, 6));
        var once = Sayilar();

        await SayfaAsync();

        Assert.Equal(once, Sayilar());
    }
}
