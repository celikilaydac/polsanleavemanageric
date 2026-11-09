using System.Net;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Microsoft.Extensions.DependencyInjection;
using PolsanIzin.Calisanlar;
using PolsanIzin.Izinler;

namespace PolsanIzin.Web.Tests;

/// <summary>Demo kullanıcılar: Development seed verisiyle aynı (spec 0002 R-16); parola yalnızca demo verisidir.</summary>
internal static class DemoKullanicilar
{
    public const string Parola = "Polsan.Demo.2026";

    public static readonly string[] Epostalar =
    [
        "genel.mudur@polsan.example",
        "ik1@polsan.example",
        "ik2@polsan.example",
        "yonetici.a@polsan.example",
        "yonetici.b@polsan.example",
        "calisan.a1@polsan.example",
        "calisan.a2@polsan.example",
        "calisan.b1@polsan.example",
    ];
}

internal static class TestYardimcilari
{
    public const string OturumCerezi = "PolsanIzin.Oturum";

    public static async Task<IHtmlDocument> HtmlAsync(this HttpResponseMessage yanit)
    {
        var icerik = await yanit.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return new HtmlParser().ParseDocument(icerik);
    }

    public static async Task<string> TokenAlAsync(this HttpClient istemci, string adres)
    {
        var yanit = await istemci.GetAsync(adres, TestContext.Current.CancellationToken);
        var belge = await yanit.HtmlAsync();
        var token = belge.QuerySelector("input[name=__RequestVerificationToken]")?.GetAttribute("value");
        Assert.False(string.IsNullOrEmpty(token), $"{adres} sayfasında antiforgery token yok (durum {yanit.StatusCode}).");
        return token!;
    }

    public static async Task<HttpResponseMessage> GirisYapAsync(
        this HttpClient istemci, string? eposta, string? parola, string? returnUrl = null)
    {
        var token = await istemci.TokenAlAsync("/Giris");
        var alanlar = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Eposta"] = eposta ?? string.Empty,
            ["Parola"] = parola ?? string.Empty,
        };
        if (returnUrl is not null)
        {
            alanlar["returnUrl"] = returnUrl;
        }

        return await istemci.PostAsync("/Giris", new FormUrlEncodedContent(alanlar), TestContext.Current.CancellationToken);
    }

    public static async Task<HttpResponseMessage> GirisYapmisAsync(this HttpClient istemci, string eposta = "ik1@polsan.example")
    {
        var yanit = await istemci.GirisYapAsync(eposta, DemoKullanicilar.Parola);
        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        return yanit;
    }

    public static Task<HttpResponseMessage> AlAsync(this HttpClient istemci, string adres) =>
        istemci.GetAsync(adres, TestContext.Current.CancellationToken);

    public static string? Konum(this HttpResponseMessage yanit) => yanit.Headers.Location?.OriginalString;

    public static bool OturumCereziYazar(this HttpResponseMessage yanit) =>
        yanit.Headers.TryGetValues("Set-Cookie", out var cerezler)
        && cerezler.Any(c => c.StartsWith(OturumCerezi + "=", StringComparison.Ordinal)
                             && !c.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase));

    public static async Task<Guid> CalisanIdAsync(this PolsanIzinFactory factory, string eposta)
    {
        using var kapsam = factory.Services.CreateScope();
        var sonuc = await kapsam.ServiceProvider.GetRequiredService<IKimlikDogrulamaServisi>()
            .DogrulaAsync(eposta, DemoKullanicilar.Parola, TestContext.Current.CancellationToken);
        return sonuc.Deger.CalisanId;
    }

    /// <summary>Public servis üzerinden geçerli bir talep oluşturur (test verisi).</summary>
    public static async Task TalepAsync(
        this PolsanIzinFactory factory, Guid calisan, IzinTuru tur, DateOnly baslangic, DateOnly bitis, string? aciklama = null)
    {
        using var kapsam = factory.Services.CreateScope();
        var sonuc = await kapsam.ServiceProvider.GetRequiredService<IIzinTalebiServisi>().TalepOlusturAsync(
            new TalepOlusturKomutu(calisan, tur, baslangic, bitis, aciklama), TestContext.Current.CancellationToken);
        Assert.True(sonuc.Basarili, string.Join("; ", sonuc.Hatalar.Select(h => h.Mesaj)));
    }
}
