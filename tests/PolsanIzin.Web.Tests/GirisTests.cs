using System.Net;

namespace PolsanIzin.Web.Tests;

[Trait("Kategori", "Entegrasyon")]
public sealed class GirisTests : IDisposable
{
    private readonly PolsanIzinFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Post_DogruBilgiler_AnaSayfayaYonlendirir()
    {
        var istemci = _factory.Istemci();

        var yanit = await istemci.GirisYapAsync("ik1@polsan.example", DemoKullanicilar.Parola);

        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        Assert.Equal("/", yanit.Konum());
        Assert.True(yanit.OturumCereziYazar());
        var anaSayfa = await istemci.AlAsync("/");
        Assert.Equal(HttpStatusCode.OK, anaSayfa.StatusCode);
    }

    [Fact]
    public async Task Post_BuyukHarfVeBoslukluEposta_GirisBasarili()
    {
        var yanit = await _factory.Istemci().GirisYapAsync("  IK1@Polsan.Example ", DemoKullanicilar.Parola);

        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        Assert.True(yanit.OturumCereziYazar());
    }

    [Theory]
    [InlineData("ik1@polsan.example", "yanlis-parola")]
    [InlineData("kayitsiz@polsan.example", DemoKullanicilar.Parola)]
    public async Task Post_HataliBilgi_GenelMesajEpostaKorunurParolaBos(string eposta, string parola)
    {
        var istemci = _factory.Istemci();

        var yanit = await istemci.GirisYapAsync(eposta, parola);

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        Assert.False(yanit.OturumCereziYazar());
        var belge = await yanit.HtmlAsync();
        Assert.Equal("E-posta veya parola hatalı", belge.QuerySelector(".hata-ozeti")?.TextContent.Trim());
        Assert.Equal(eposta, belge.QuerySelector("input#Eposta")?.GetAttribute("value"));
        Assert.True(string.IsNullOrEmpty(belge.QuerySelector("input#Parola")?.GetAttribute("value")));
        Assert.Equal(HttpStatusCode.Redirect, (await istemci.AlAsync("/")).StatusCode);
    }

    [Theory]
    [InlineData("", "", true, true)]
    [InlineData("ik1@polsan.example", "", false, true)]
    [InlineData("", DemoKullanicilar.Parola, true, false)]
    public async Task Post_BosAlanlar_AlanMesajlari(string eposta, string parola, bool epostaHatasi, bool parolaHatasi)
    {
        var yanit = await _factory.Istemci().GirisYapAsync(eposta, parola);

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        Assert.False(yanit.OturumCereziYazar());
        var belge = await yanit.HtmlAsync();
        Assert.Equal(epostaHatasi ? "E-posta zorunludur." : string.Empty, belge.QuerySelector("#Eposta-hata")!.TextContent.Trim());
        Assert.Equal(parolaHatasi ? "Parola zorunludur." : string.Empty, belge.QuerySelector("#Parola-hata")!.TextContent.Trim());
    }

    [Fact]
    public async Task Get_Erisilebilirlik_EtiketHataBaglantisiParolaGizli()
    {
        var belge = await (await _factory.Istemci().AlAsync("/Giris")).HtmlAsync();

        foreach (var alan in new[] { "Eposta", "Parola" })
        {
            var girdi = belge.QuerySelector($"input#{alan}");
            Assert.NotNull(girdi);
            Assert.NotNull(belge.QuerySelector($"label[for={alan}]"));
            Assert.False(string.IsNullOrWhiteSpace(belge.QuerySelector($"label[for={alan}]")!.TextContent));
            var aciklama = girdi.GetAttribute("aria-describedby");
            Assert.Equal($"{alan}-hata", aciklama);
            Assert.NotNull(belge.GetElementById(aciklama!));
        }

        Assert.Equal("password", belge.QuerySelector("input#Parola")!.GetAttribute("type"));
        Assert.Equal("post", belge.QuerySelector("main form")!.GetAttribute("method"));
        Assert.Equal("submit", belge.QuerySelector("main form button")!.GetAttribute("type"));
        // Akış JS gerektirmez: sayfada script yok.
        Assert.Empty(belge.QuerySelectorAll("script"));
        // Oturumsuz kullanıcıya menü gösterilmez.
        Assert.Null(belge.QuerySelector("nav"));
    }

    [Fact]
    public async Task Post_HataliAlan_AriaInvalidIsaretlenir()
    {
        var belge = await (await _factory.Istemci().GirisYapAsync("", "")).HtmlAsync();

        Assert.Equal("true", belge.QuerySelector("input#Eposta")!.GetAttribute("aria-invalid"));
        Assert.Equal("true", belge.QuerySelector("input#Parola")!.GetAttribute("aria-invalid"));
    }

    [Fact]
    public async Task Loglar_KisiselVeriVeParolaIcermez()
    {
        var istemci = _factory.Istemci();
        await istemci.GirisYapAsync("ik1@polsan.example", "yanlis-parola");
        await istemci.GirisYapAsync("kayitsiz@polsan.example", "baska-yanlis");
        await istemci.GirisYapAsync("ik1@polsan.example", DemoKullanicilar.Parola);
        await istemci.AlAsync("/");

        var kayitlar = _factory.Loglar.Kayitlar;
        Assert.NotEmpty(kayitlar);
        string[] yasaklar = ["ik1@polsan.example", "kayitsiz@polsan.example", "Elif", "Kaya", DemoKullanicilar.Parola, "yanlis-parola", "baska-yanlis"];
        foreach (var yasak in yasaklar)
        {
            Assert.DoesNotContain(kayitlar, k => k.Contains(yasak, StringComparison.OrdinalIgnoreCase));
        }

        Assert.Contains(kayitlar, k => k.Contains("Oturum açıldı", StringComparison.Ordinal));
    }
}

[Trait("Kategori", "Entegrasyon")]
public sealed class GirisSiniriTests
{
    [Fact]
    public async Task Post_OnBirinciDeneme_429VeMesaj()
    {
        using var factory = new PolsanIzinFactory { DenemeSiniri = 10 };
        var istemci = factory.Istemci();

        for (var i = 1; i <= 10; i++)
        {
            var deneme = await istemci.GirisYapAsync("ik1@polsan.example", "yanlis-parola");
            Assert.Equal(HttpStatusCode.OK, deneme.StatusCode);
        }

        // 11. deneme, doğru parolayla bile reddedilir.
        var yanit = await istemci.GirisYapAsync("ik1@polsan.example", DemoKullanicilar.Parola);

        Assert.Equal(HttpStatusCode.TooManyRequests, yanit.StatusCode);
        Assert.False(yanit.OturumCereziYazar());
        var belge = await yanit.HtmlAsync();
        Assert.Equal("Çok fazla deneme, lütfen biraz sonra tekrar deneyin", belge.QuerySelector(".hata-ozeti")?.TextContent.Trim());
        Assert.NotNull(belge.QuerySelector("main form"));
    }

    [Fact]
    public async Task Post_SonrakiPencere_YenidenIslenir()
    {
        // Pencere TimeProvider ile ölçülür; zaman FakeTimeProvider ile ilerletilir (gerçek bekleme yok).
        using var factory = new PolsanIzinFactory { DenemeSiniri = 2 };
        var istemci = factory.Istemci();

        await istemci.GirisYapAsync("ik1@polsan.example", "yanlis-parola");
        await istemci.GirisYapAsync("ik1@polsan.example", "yanlis-parola");
        Assert.Equal(HttpStatusCode.TooManyRequests, (await istemci.GirisYapAsync("ik1@polsan.example", DemoKullanicilar.Parola)).StatusCode);

        // Pencere bitmeden bir an önce hâlâ reddedilir.
        factory.Zaman.Advance(factory.Pencere - TimeSpan.FromTicks(1));
        Assert.Equal(HttpStatusCode.TooManyRequests, (await istemci.GirisYapAsync("ik1@polsan.example", DemoKullanicilar.Parola)).StatusCode);

        factory.Zaman.Advance(TimeSpan.FromTicks(1));

        var yanit = await istemci.GirisYapAsync("ik1@polsan.example", DemoKullanicilar.Parola);
        Assert.Equal(HttpStatusCode.Redirect, yanit.StatusCode);
        Assert.True(yanit.OturumCereziYazar());
    }
}
