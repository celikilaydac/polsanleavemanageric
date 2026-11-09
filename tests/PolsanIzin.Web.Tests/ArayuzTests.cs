using System.Net;
using System.Text.RegularExpressions;
using AngleSharp.Html.Dom;
using PolsanIzin.Izinler;

namespace PolsanIzin.Web.Tests;

/// <summary>Arayüz kuralları (spec 0005 CB-3, CB-4, PB-3) — görünümün ölçülebilen kısmı.</summary>
[Trait("Kategori", "Entegrasyon")]
public sealed partial class ArayuzTests : IDisposable
{
    private readonly PolsanIzinFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private async Task<HttpClient> GirisYapmisIstemciAsync()
    {
        var istemci = _factory.Istemci();
        await istemci.GirisYapmisAsync();
        return istemci;
    }

    private static async Task<IHtmlDocument> SayfaAsync(HttpClient istemci, string adres)
    {
        var yanit = await istemci.AlAsync(adres);
        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        return await yanit.HtmlAsync();
    }

    [Fact]
    public async Task Get_Taleplerim_DurumRozetMetinli()
    {
        var ik1 = await _factory.CalisanIdAsync("ik1@polsan.example");
        await _factory.TalepAsync(ik1, IzinTuru.Mazeret, new(2026, 11, 2), new(2026, 11, 2));
        await _factory.TalepAsync(ik1, IzinTuru.Mazeret, new(2026, 11, 3), new(2026, 11, 3));
        _factory.Calistir("UPDATE \"IzinTalepleri\" SET \"Durum\" = 'IptalEdildi' WHERE \"Baslangic\" = '2026-11-03'");

        var belge = await SayfaAsync(await GirisYapmisIstemciAsync(), "/Izinler/Taleplerim");

        var rozetler = belge.QuerySelectorAll("#talepler td .rozet").ToList();
        Assert.Equal(["İptal edildi", "Beklemede"], rozetler.Select(r => r.TextContent.Trim()));
        Assert.Equal(["rozet-iptaledildi", "rozet-beklemede"], rozetler.Select(r => r.ClassList.Single(c => c != "rozet")));
    }

    [Fact]
    public async Task Get_TumSayfalar_DisKaynakVeScriptYok()
    {
        var anonim = await SayfaAsync(_factory.Istemci(), "/Giris");
        var istemci = await GirisYapmisIstemciAsync();
        List<IHtmlDocument> belgeler = [anonim];
        foreach (var adres in new[] { "/", "/Izinler/Olustur", "/Izinler/Taleplerim", "/Izinler/Gecmis" })
        {
            belgeler.Add(await SayfaAsync(istemci, adres));
        }

        foreach (var belge in belgeler)
        {
            Assert.Empty(belge.QuerySelectorAll("script"));
            var kaynaklar = belge.QuerySelectorAll("link[href], img[src], iframe[src], source[src]")
                .Select(e => e.GetAttribute("href") ?? e.GetAttribute("src")!)
                .ToList();
            Assert.All(kaynaklar, k => Assert.True(k.StartsWith('/') && !k.StartsWith("//", StringComparison.Ordinal), $"Dış kaynak: {k}"));
            Assert.Single(kaynaklar, k => k.StartsWith("/css/site.css", StringComparison.Ordinal));
        }
    }

    [Fact]
    public async Task Get_UstBar_AcikSekmeRenkDisindaDaAyirt()
    {
        var belge = await SayfaAsync(await GirisYapmisIstemciAsync(), "/Izinler/Gecmis");
        Assert.Equal("Geçmiş", belge.QuerySelector("header nav a[aria-current=page]")?.TextContent.Trim());
        Assert.Equal("PolsanIzin", belge.QuerySelector("header .uygulama-adi")?.TextContent.Trim());

        // Açık sekme kuralı renk dışında kalınlık ve alt çizgi de taşır (CB-3, PB-4).
        var css = await (await _factory.Istemci().AlAsync("/css/site.css")).Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var kural = AcikSekmeKurali().Match(css);
        Assert.True(kural.Success, "Açık sekme kuralı yok.");
        Assert.Contains("font-weight: 700", kural.Groups["govde"].Value, StringComparison.Ordinal);
        Assert.Contains("text-decoration: underline", kural.Groups["govde"].Value, StringComparison.Ordinal);
    }

    [GeneratedRegex("""nav a\[aria-current="page"\]\s*\{(?<govde>[^}]*)\}""")]
    private static partial Regex AcikSekmeKurali();
}
