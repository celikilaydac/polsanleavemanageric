using System.Net;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Microsoft.Extensions.DependencyInjection;
using PolsanIzin.Bakiyeler;
using PolsanIzin.Calisanlar;
using PolsanIzin.Izinler;

namespace PolsanIzin.Web.Tests;

/// <summary>
/// İzin talebi giriş ekranı, gerçek modüllerle (spec 0001). "Bugün" = 2026-10-21 (factory saati);
/// demo verisinde 2026 hakkı 14 gün, 2026-10-29 tatil.
/// </summary>
[Trait("Kategori", "Entegrasyon")]
public sealed class IzinTalebiOlusturSayfasiTests : IDisposable
{
    private const string _adres = "/Izinler/Olustur";

    private readonly PolsanIzinFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private static CancellationToken Iptal => TestContext.Current.CancellationToken;

    private long TalepSayisi() => _factory.SorguSayisi("SELECT COUNT(*) FROM \"IzinTalepleri\"");

    private long DenetimSayisi() => _factory.SorguSayisi("SELECT COUNT(*) FROM \"DenetimKayitlari\"");

    private long RezerveSayisi() => _factory.SorguSayisi("SELECT COUNT(*) FROM \"BakiyeHareketleri\"");

    private async Task<Guid> CalisanIdAsync(string eposta)
    {
        using var kapsam = _factory.Services.CreateScope();
        var sonuc = await kapsam.ServiceProvider.GetRequiredService<IKimlikDogrulamaServisi>()
            .DogrulaAsync(eposta, DemoKullanicilar.Parola, Iptal);
        return sonuc.Deger.CalisanId;
    }

    private async Task<HttpClient> GirisYapmisIstemciAsync(string eposta = "ik1@polsan.example")
    {
        var istemci = _factory.Istemci();
        await istemci.GirisYapmisAsync(eposta);
        return istemci;
    }

    private static async Task<HttpResponseMessage> GonderAsync(HttpClient istemci, IDictionary<string, string> alanlar)
    {
        var form = new Dictionary<string, string>(alanlar)
        {
            ["__RequestVerificationToken"] = await istemci.TokenAlAsync(_adres),
        };
        return await istemci.PostAsync(_adres, new FormUrlEncodedContent(form), Iptal);
    }

    private static Dictionary<string, string> Form(string tur, string baslangic, string bitis, string aciklama = "") => new()
    {
        ["IzinTuru"] = tur,
        ["Baslangic"] = baslangic,
        ["Bitis"] = bitis,
        ["Aciklama"] = aciklama,
    };

    private static string Metin(IHtmlDocument belge, string secici) =>
        belge.QuerySelector(secici)?.TextContent.Trim() ?? string.Empty;

    private static void FormBos(IHtmlDocument belge)
    {
        Assert.Equal(string.Empty, belge.QuerySelector<IHtmlSelectElement>("#IzinTuru")!.Value);
        Assert.Equal(string.Empty, belge.QuerySelector<IHtmlInputElement>("#Baslangic")!.Value);
        Assert.Equal(string.Empty, belge.QuerySelector<IHtmlInputElement>("#Bitis")!.Value);
        Assert.Equal(string.Empty, belge.QuerySelector<IHtmlTextAreaElement>("#Aciklama")!.Value);
    }

    [Fact]
    public async Task Post_GecerliYillik_BeklemedeOzetVeBakiyeDuser()
    {
        var istemci = await GirisYapmisIstemciAsync();

        var yanit = await GonderAsync(istemci, Form("Yillik", "2026-11-02", "2026-11-06"));

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        var belge = await yanit.HtmlAsync();
        var ozet = Metin(belge, "#basari");
        Assert.Contains("Talebiniz oluşturuldu", ozet, StringComparison.Ordinal);
        Assert.Equal(
            ["Yıllık", "02.11.2026", "06.11.2026", "5", "Beklemede"],
            belge.QuerySelectorAll("#basari dd").Select(d => d.TextContent.Trim()));
        Assert.Contains("9 gün", Metin(belge, "#bakiye"), StringComparison.Ordinal);
        FormBos(belge);
        Assert.Equal(1, TalepSayisi());
    }

    [Fact]
    public async Task Post_Mazeret_BakiyeDegismez()
    {
        var istemci = await GirisYapmisIstemciAsync();

        var belge = await (await GonderAsync(istemci, Form("Mazeret", "2026-11-02", "2026-11-06"))).HtmlAsync();

        Assert.Contains("Talebiniz oluşturuldu", Metin(belge, "#basari"), StringComparison.Ordinal);
        Assert.Contains("14 gün", Metin(belge, "#bakiye"), StringComparison.Ordinal);
        Assert.Equal(0, RezerveSayisi());
    }

    [Fact]
    public async Task Post_ZorunluAlanlarBos_HerAlanIcinMesaj()
    {
        var istemci = await GirisYapmisIstemciAsync();

        var yanit = await GonderAsync(istemci, Form("", "", ""));

        var belge = await yanit.HtmlAsync();
        Assert.Equal("İzin türü seçilmelidir.", Metin(belge, "#IzinTuru-hata"));
        Assert.Equal("Başlangıç tarihi zorunludur.", Metin(belge, "#Baslangic-hata"));
        Assert.Equal("Bitiş tarihi zorunludur.", Metin(belge, "#Bitis-hata"));
        Assert.Null(belge.QuerySelector("#basari"));
        Assert.Equal(0, TalepSayisi());
    }

    [Theory]
    [InlineData("Hafta", "2026-11-02", "2026-11-02", "#IzinTuru-hata", "Geçersiz izin türü.")]
    [InlineData("1", "2026-11-02", "2026-11-02", "#IzinTuru-hata", "Geçersiz izin türü.")]
    [InlineData("Mazeret", "2026-13-45", "2026-11-02", "#Baslangic-hata", "Geçerli bir tarih girin.")]
    [InlineData("Mazeret", "2026-11-02", "02.11.2026", "#Bitis-hata", "Geçerli bir tarih girin.")]
    public async Task Post_GecersizTarihVeTur_Reddedilir(string tur, string baslangic, string bitis, string alan, string mesaj)
    {
        var istemci = await GirisYapmisIstemciAsync();

        var belge = await (await GonderAsync(istemci, Form(tur, baslangic, bitis))).HtmlAsync();

        Assert.Equal(mesaj, Metin(belge, alan));
        Assert.Equal(0, TalepSayisi());
    }

    [Fact]
    public async Task Post_AyniFormIkiKez_IkinciBR3IleReddedilir()
    {
        var istemci = await GirisYapmisIstemciAsync();
        var form = Form("Yillik", "2026-11-02", "2026-11-06");

        await GonderAsync(istemci, form);
        var ikinci = await (await GonderAsync(istemci, form)).HtmlAsync();

        Assert.Contains("çakışan", Metin(ikinci, "#genel-hata"), StringComparison.Ordinal);
        Assert.Equal(1, TalepSayisi());
        Assert.Equal(1, RezerveSayisi());
    }

    [Fact]
    public async Task TalepOlustur_EsZamanliIkiYillik_BakiyeNegatifeDusmez()
    {
        var calisan = await CalisanIdAsync("ik1@polsan.example");

        // Her biri tek başına sığar (10 iş günü ≤ 14), ikisi birlikte sığmaz.
        async Task<bool> OlusturAsync(DateOnly baslangic, DateOnly bitis)
        {
            using var kapsam = _factory.Services.CreateScope();
            var sonuc = await kapsam.ServiceProvider.GetRequiredService<IIzinTalebiServisi>().TalepOlusturAsync(
                new TalepOlusturKomutu(calisan, IzinTuru.Yillik, baslangic, bitis, null), Iptal);
            return sonuc.Basarili;
        }

        var sonuclar = await Task.WhenAll(
            OlusturAsync(new(2026, 11, 2), new(2026, 11, 13)),
            OlusturAsync(new(2026, 11, 16), new(2026, 11, 27)));

        Assert.Single(sonuclar, s => s);
        using var kapsam = _factory.Services.CreateScope();
        Assert.Equal(4, await kapsam.ServiceProvider.GetRequiredService<IBakiyeServisi>().KullanilabilirBakiyeAsync(calisan, 2026, Iptal));
    }

    [Fact]
    public async Task Post_CokluIhlal_TumMesajlarVeDegerlerKorunur()
    {
        var istemci = await GirisYapmisIstemciAsync();
        var aciklama = new string('a', 501);

        var belge = await (await GonderAsync(istemci, Form("Mazeret", "2026-10-20", "2026-10-21", aciklama))).HtmlAsync();

        Assert.Equal("Başlangıç tarihi bugünden önce olamaz.", Metin(belge, "#Baslangic-hata"));
        Assert.Equal("Açıklama en fazla 500 karakter olabilir.", Metin(belge, "#Aciklama-hata"));
        Assert.Equal("Mazeret", belge.QuerySelector<IHtmlSelectElement>("#IzinTuru")!.Value);
        Assert.Equal("2026-10-20", belge.QuerySelector<IHtmlInputElement>("#Baslangic")!.Value);
        Assert.Equal("2026-10-21", belge.QuerySelector<IHtmlInputElement>("#Bitis")!.Value);
        Assert.Equal(aciklama, belge.QuerySelector<IHtmlTextAreaElement>("#Aciklama")!.Value);
        Assert.Equal((0, 0, 0), (TalepSayisi(), DenetimSayisi(), RezerveSayisi()));
    }

    [Fact]
    public async Task GetVePost_OturumYok_GiriseYonlendirirTalepOlusmaz()
    {
        var istemci = _factory.Istemci();

        var get = await istemci.AlAsync(_adres);
        var post = await istemci.PostAsync(_adres, new FormUrlEncodedContent(Form("Mazeret", "2026-11-02", "2026-11-02")), Iptal);

        Assert.Equal(HttpStatusCode.Redirect, get.StatusCode);
        Assert.StartsWith("https://localhost/Giris", get.Konum(), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        Assert.StartsWith("https://localhost/Giris", post.Konum(), StringComparison.Ordinal);
        Assert.Equal(0, TalepSayisi());
    }

    [Fact]
    public async Task Post_BaskaCalisanIdEklenir_OturumdakiAdinaOlusur()
    {
        var oturumdaki = await CalisanIdAsync("ik1@polsan.example");
        var baskasi = await CalisanIdAsync("calisan.a1@polsan.example");
        var istemci = await GirisYapmisIstemciAsync();
        var form = Form("Mazeret", "2026-11-02", "2026-11-02");
        form["CalisanId"] = baskasi.ToString();

        await GonderAsync(istemci, form);

        long Sayi(Guid id) => _factory.SorguSayisi(
            $"SELECT COUNT(*) FROM \"IzinTalepleri\" WHERE upper(\"CalisanId\") = upper('{id}')");
        Assert.Equal(1, Sayi(oturumdaki));
        Assert.Equal(0, Sayi(baskasi));
    }

    [Theory]
    [InlineData("yonetici.a@polsan.example")]
    [InlineData("ik2@polsan.example")]
    public async Task Post_YoneticiVeIk_KendiAdinaOlusturur(string eposta)
    {
        var calisan = await CalisanIdAsync(eposta);
        var istemci = await GirisYapmisIstemciAsync(eposta);

        var belge = await (await GonderAsync(istemci, Form("Yillik", "2026-11-02", "2026-11-03"))).HtmlAsync();

        Assert.Contains("Talebiniz oluşturuldu", Metin(belge, "#basari"), StringComparison.Ordinal);
        Assert.Equal(1, _factory.SorguSayisi(
            $"SELECT COUNT(*) FROM \"IzinTalepleri\" WHERE upper(\"CalisanId\") = upper('{calisan}')"));
    }

    [Fact]
    public async Task Get_IlkAcilis_BosFormVeBakiye()
    {
        var istemci = await GirisYapmisIstemciAsync();

        var belge = await (await istemci.AlAsync(_adres)).HtmlAsync();

        FormBos(belge);
        Assert.Equal("Seçiniz", belge.QuerySelector<IHtmlSelectElement>("#IzinTuru")!.SelectedOptions.Single().TextContent);
        Assert.Contains("2026", Metin(belge, "#bakiye"), StringComparison.Ordinal);
        Assert.Contains("14 gün", Metin(belge, "#bakiye"), StringComparison.Ordinal);
        Assert.Null(belge.QuerySelector("#basari"));
        Assert.Null(belge.QuerySelector("#genel-hata"));
    }

    [Fact]
    public async Task Get_HakTanimsiz_BilgiMetni()
    {
        _factory.Zaman.SetUtcNow(new DateTimeOffset(2027, 1, 4, 9, 0, 0, TimeSpan.Zero));
        var istemci = await GirisYapmisIstemciAsync();

        var belge = await (await istemci.AlAsync(_adres)).HtmlAsync();

        Assert.Equal("Bu yıl için izin hakkınız tanımlı değil", Metin(belge, "#bakiye"));
    }

    [Fact]
    public async Task Get_Erisilebilirlik_EtiketVeHataBaglantilari()
    {
        var istemci = await GirisYapmisIstemciAsync();

        var belge = await (await GonderAsync(istemci, Form("", "", ""))).HtmlAsync();

        foreach (var alan in new[] { "IzinTuru", "Baslangic", "Bitis", "Aciklama" })
        {
            var etiket = belge.QuerySelector($"label[for={alan}]");
            Assert.False(string.IsNullOrWhiteSpace(etiket?.TextContent), $"{alan} için görünür etiket yok.");
            var kontrol = belge.GetElementById(alan)!;
            var hataId = kontrol.GetAttribute("aria-describedby")!;
            Assert.NotNull(belge.GetElementById(hataId));
        }

        Assert.Equal("true", belge.GetElementById("IzinTuru")!.GetAttribute("aria-invalid"));
        Assert.Equal("false", belge.GetElementById("Aciklama")!.GetAttribute("aria-invalid"));
        var form = Assert.Single(belge.QuerySelectorAll("main form"));
        Assert.Equal("post", form.GetAttribute("method"));
        Assert.Equal("Talep Oluştur", form.QuerySelector("button[type=submit]")?.TextContent.Trim());
        Assert.Empty(belge.QuerySelectorAll("script"));
    }
}
