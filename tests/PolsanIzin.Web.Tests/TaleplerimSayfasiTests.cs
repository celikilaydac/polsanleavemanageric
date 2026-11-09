using System.Net;
using AngleSharp.Html.Dom;
using PolsanIzin.Izinler;

namespace PolsanIzin.Web.Tests;

/// <summary>Taleplerim sekmesi (spec 0004). "Bugün" = 2026-10-21 (factory saati).</summary>
[Trait("Kategori", "Entegrasyon")]
public sealed class TaleplerimSayfasiTests : IDisposable
{
    private const string _adres = "/Izinler/Taleplerim";

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
        .. belge.QuerySelectorAll("#talepler tbody tr")
            .Select(tr => tr.QuerySelectorAll("td").Select(td => td.TextContent.Trim()).ToArray()),
    ];

    private (long, long, long) Sayilar() => (
        _factory.SorguSayisi("SELECT COUNT(*) FROM \"IzinTalepleri\""),
        _factory.SorguSayisi("SELECT COUNT(*) FROM \"DenetimKayitlari\""),
        _factory.SorguSayisi("SELECT COUNT(*) FROM \"BakiyeHareketleri\""));

    [Fact]
    public async Task Get_UcTalep_SutunlarVeSira()
    {
        var ik1 = await _factory.CalisanIdAsync("ik1@polsan.example");
        await _factory.TalepAsync(ik1, IzinTuru.Yillik, T(11, 2), T(11, 6), "Aile ziyareti");
        await _factory.TalepAsync(ik1, IzinTuru.Mazeret, T(11, 16), T(11, 16), "Taşınma");
        await _factory.TalepAsync(ik1, IzinTuru.Ucretsiz, T(12, 1), T(12, 2), "Kurs");

        var belge = await SayfaAsync();

        Assert.Equal(
            [
                ["Ücretsiz", "01.12.2026", "02.12.2026", "2", "Beklemede", "Kurs"],
                ["Mazeret", "16.11.2026", "16.11.2026", "1", "Beklemede", "Taşınma"],
                ["Yıllık", "02.11.2026", "06.11.2026", "5", "Beklemede", "Aile ziyareti"],
            ],
            Satirlar(belge));
    }

    [Fact]
    public async Task Get_DigerDurumlar_TurkceAd()
    {
        var ik1 = await _factory.CalisanIdAsync("ik1@polsan.example");
        await _factory.TalepAsync(ik1, IzinTuru.Mazeret, T(11, 2), T(11, 2));
        await _factory.TalepAsync(ik1, IzinTuru.Mazeret, T(11, 3), T(11, 3));
        await _factory.TalepAsync(ik1, IzinTuru.Mazeret, T(11, 4), T(11, 4));
        // Onay / ret / iptal henüz public API'de yok; durumlar doğrudan kurulur (plan 0004 Risk 2).
        _factory.Calistir("UPDATE \"IzinTalepleri\" SET \"Durum\" = 'Onaylandi' WHERE \"Baslangic\" = '2026-11-02'");
        _factory.Calistir("UPDATE \"IzinTalepleri\" SET \"Durum\" = 'Reddedildi' WHERE \"Baslangic\" = '2026-11-03'");
        _factory.Calistir("UPDATE \"IzinTalepleri\" SET \"Durum\" = 'IptalEdildi' WHERE \"Baslangic\" = '2026-11-04'");

        var belge = await SayfaAsync();

        Assert.Equal(["İptal edildi", "Reddedildi", "Onaylandı"], Satirlar(belge).Select(s => s[4]));
    }

    [Fact]
    public async Task Get_AciklamaBosVeIsaretleme_TireVeDuzMetin()
    {
        var ik1 = await _factory.CalisanIdAsync("ik1@polsan.example");
        await _factory.TalepAsync(ik1, IzinTuru.Mazeret, T(11, 2), T(11, 2));
        await _factory.TalepAsync(ik1, IzinTuru.Mazeret, T(11, 3), T(11, 3), "<b>kalın</b>");

        var belge = await SayfaAsync();

        Assert.Equal(["<b>kalın</b>", "—"], Satirlar(belge).Select(s => s[5]));
        Assert.Empty(belge.QuerySelectorAll("#talepler b"));
    }

    [Fact]
    public async Task Get_BaskaCalisan_GorunmezAdresParametresiEtkisiz()
    {
        var ik1 = await _factory.CalisanIdAsync("ik1@polsan.example");
        var a1 = await _factory.CalisanIdAsync("calisan.a1@polsan.example");
        await _factory.TalepAsync(ik1, IzinTuru.Mazeret, T(11, 2), T(11, 2));
        await _factory.TalepAsync(a1, IzinTuru.Ucretsiz, T(11, 9), T(11, 9));

        var belge = await SayfaAsync(adres: $"{_adres}?calisanId={a1}&CalisanId={a1}");

        var satir = Assert.Single(Satirlar(belge));
        Assert.Equal("02.11.2026", satir[1]);
    }

    [Fact]
    public async Task Get_TalepYok_BosDurum()
    {
        var belge = await SayfaAsync();

        Assert.Equal("Henüz izin talebiniz yok.", belge.GetElementById("bos")?.TextContent.Trim());
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
    public async Task Get_Menu_AcikSekmeIsaretli()
    {
        var belge = await SayfaAsync();

        var acik = Assert.Single(belge.QuerySelectorAll("nav a[aria-current]"));
        Assert.Equal("page", acik.GetAttribute("aria-current"));
        Assert.Equal(_adres, acik.GetAttribute("href"));
        Assert.Equal("Taleplerim", acik.TextContent.Trim());
    }

    [Fact]
    public async Task Get_Erisilebilirlik_CaptionVeBasliklar()
    {
        var ik1 = await _factory.CalisanIdAsync("ik1@polsan.example");
        await _factory.TalepAsync(ik1, IzinTuru.Mazeret, T(11, 2), T(11, 2));

        var belge = await SayfaAsync();

        Assert.False(string.IsNullOrWhiteSpace(belge.QuerySelector("#talepler caption")?.TextContent));
        Assert.Equal(
            ["İzin türü", "Başlangıç", "Bitiş", "İş günü", "Durum", "Açıklama"],
            belge.QuerySelectorAll("#talepler th[scope=col]").Select(th => th.TextContent.Trim()));
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
