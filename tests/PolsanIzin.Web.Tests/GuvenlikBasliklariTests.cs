using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PolsanIzin.Calisanlar;

namespace PolsanIzin.Web.Tests;

/// <summary>Güvenlik başlıkları ve çerez bayrakları (docs/security.md, docs/testing.md §6).</summary>
[Trait("Kategori", "Entegrasyon")]
public sealed class GuvenlikBasliklariTests
{
    [Theory]
    [InlineData("/Giris", HttpStatusCode.OK)]
    [InlineData("/", HttpStatusCode.Redirect)]
    [InlineData("/css/site.css", HttpStatusCode.OK)]
    public async Task Get_HerYanit_GuvenlikBasliklariVar(string adres, HttpStatusCode beklenen)
    {
        using var factory = new PolsanIzinFactory();

        var yanit = await factory.Istemci().AlAsync(adres);

        Assert.Equal(beklenen, yanit.StatusCode);
        BasliklariDogrula(yanit);
    }

    [Fact]
    public async Task Hata_ProductionOrtami_HataSayfasindaGuvenlikBasliklariVar()
    {
        using var factory = new PolsanIzinFactory { Ortam = "Production", TestVerisiYukle = false }
            .ServisEkle(s => s.AddScoped<IKimlikDogrulamaServisi, HataSayfasiTests.PatlayanKimlikDogrulama>());

        var yanit = await factory.Istemci().GirisYapAsync("ik1@polsan.example", "herhangi");

        Assert.Equal(HttpStatusCode.InternalServerError, yanit.StatusCode);
        BasliklariDogrula(yanit);
    }

    /// <summary>
    /// Düz HTTP isteğinde de <c>Secure</c>: <c>SecurePolicy=Always</c>'ı <c>SameAsRequest</c>'ten
    /// ayıran durum budur (VERIFY V-2).
    /// </summary>
    [Theory]
    [InlineData("https://localhost")]
    [InlineData("http://localhost")]
    public async Task Giris_ProductionOrtami_OturumCereziSecureHttpOnlyLax(string tabanAdres)
    {
        using var factory = new PolsanIzinFactory { Ortam = "Production" };
        var istemci = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri(tabanAdres),
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        var yanit = await istemci.GirisYapmisAsync();

        var cerez = Assert.Single(
            yanit.Headers.GetValues("Set-Cookie"),
            c => c.StartsWith(TestYardimcilari.OturumCerezi + "=", StringComparison.Ordinal));
        var bayraklar = cerez.Split(';').Select(b => b.Trim().ToLowerInvariant()).ToList();
        Assert.Contains("secure", bayraklar);
        Assert.Contains("httponly", bayraklar);
        Assert.Contains("samesite=lax", bayraklar);
    }

    [Fact]
    public async Task Get_DevelopmentDisindaTanimsizHost_Reddedilir()
    {
        using var factory = new PolsanIzinFactory { Ortam = "Production" };
        var istemci = factory.Istemci();
        istemci.DefaultRequestHeaders.Host = "kotu.example";

        var yanit = await istemci.AlAsync("/Giris");

        Assert.Equal(HttpStatusCode.BadRequest, yanit.StatusCode);
    }

    private static void BasliklariDogrula(HttpResponseMessage yanit)
    {
        Assert.Equal("nosniff", Assert.Single(yanit.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal("DENY", Assert.Single(yanit.Headers.GetValues("X-Frame-Options")));
        Assert.Equal("same-origin", Assert.Single(yanit.Headers.GetValues("Referrer-Policy")));
    }
}
